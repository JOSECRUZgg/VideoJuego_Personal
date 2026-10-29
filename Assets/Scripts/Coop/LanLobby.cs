using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using UnityEngine;

/// <summary>Two-seat LAN lobby. Gameplay stays disabled until the world is network-authoritative.</summary>
public sealed class LanLobby : MonoBehaviour
{
    public enum Seat { Offline, Host, Guest }
    public const int Port = 28049;
    const string ProtocolVersion = "2";
    public static LanLobby Instance { get; private set; }
    public Seat Role { get; private set; }
    public bool Connected { get; private set; }
    public bool LocalReady { get; private set; }
    public bool PeerReady { get; private set; }
    public string PeerCharacter { get; private set; } = "";
    public string Status { get; private set; } = "Sin conexión LAN";
    public string HostAddress { get; private set; } = "";
    public event Action<string[]> RunMessage;

    readonly ConcurrentQueue<Action> pending = new ConcurrentQueue<Action>();
    readonly object writeLock = new object();
    TcpListener listener;
    TcpClient socket;
    StreamWriter writer;
    int generation;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
    static void ResetStatic() { Instance = null; }

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
    }

    void Update()
    {
        while (pending.TryDequeue(out var action)) action();
    }

    void OnDestroy()
    {
        Stop();
        if (Instance == this) Instance = null;
    }

    public void Host()
    {
        Stop();
        Role = Seat.Host;
        HostAddress = LocalIPv4();
        int run = generation;
        try
        {
            listener = new TcpListener(IPAddress.Any, Port);
            listener.Start(1);
            Status = "Esperando al segundo jugador en el puerto " + Port;
            var activeListener = listener;
            ThreadPool.QueueUserWorkItem(_ =>
            {
                try
                {
                    while (run == Volatile.Read(ref generation))
                    {
                        var accepted = activeListener.AcceptTcpClient();
                        if (!IsLanAddress(((IPEndPoint)accepted.Client.RemoteEndPoint).Address))
                        { accepted.Close(); continue; }
                        pending.Enqueue(() =>
                        {
                            if (run != generation || Role != Seat.Host || Connected) { accepted.Close(); return; }
                            Attach(accepted, run);
                        });
                        return;
                    }
                }
                catch (Exception ex) { pending.Enqueue(() => Fail(run, ex)); }
            });
        }
        catch (Exception ex) { Fail(run, ex); }
    }

    public void Join(string address)
    {
        Stop();
        if (!IPAddress.TryParse(address, out var ip) || !IsLanAddress(ip))
        {
            Status = "Escribe una IPv4 de tu red local o VPN privada";
            return;
        }
        Role = Seat.Guest;
        HostAddress = address;
        Status = "Conectando con " + address + "…";
        int run = generation;
        ThreadPool.QueueUserWorkItem(_ =>
        {
            TcpClient candidate = null;
            try
            {
                candidate = new TcpClient(AddressFamily.InterNetwork);
                IAsyncResult result = candidate.BeginConnect(ip, Port, null, null);
                bool reached = result.AsyncWaitHandle.WaitOne(5000);
                result.AsyncWaitHandle.Close();
                if (!reached) throw new TimeoutException("El anfitrión no respondió");
                candidate.EndConnect(result);
                var connected = candidate;
                pending.Enqueue(() =>
                {
                    if (run != generation || Role != Seat.Guest) { connected.Close(); return; }
                    Attach(connected, run);
                });
            }
            catch (Exception ex)
            {
                candidate?.Close();
                pending.Enqueue(() => Fail(run, ex));
            }
        });
    }

    void Attach(TcpClient client, int run)
    {
        try
        {
            socket = client;
            socket.NoDelay = true;
            if (Role == Seat.Host) { listener?.Stop(); listener = null; }
            var stream = socket.GetStream();
            writer = new StreamWriter(stream, new UTF8Encoding(false)) { AutoFlush = true };
            Connected = true;
            Status = "Segundo jugador conectado";
            Send("HELLO", ProtocolVersion, LocalCharacter());
            if (Role == Seat.Host) SendConfiguration();
            ThreadPool.QueueUserWorkItem(_ => ReadPeer(stream, run));
        }
        catch (Exception ex) { Fail(run, ex); }
    }

    void ReadPeer(NetworkStream stream, int run)
    {
        try
        {
            using (var reader = new StreamReader(stream, Encoding.UTF8, false, 1024, true))
            {
                string line;
                while ((line = ReadLineLimited(reader)) != null)
                {
                    string received = line;
                    pending.Enqueue(() => { if (run == generation) Receive(received); });
                }
            }
            pending.Enqueue(() => LostPeer(run));
        }
        catch (Exception) { pending.Enqueue(() => LostPeer(run)); }
    }

    static string ReadLineLimited(StreamReader reader)
    {
        var line = new StringBuilder(128);
        int next;
        while ((next = reader.Read()) != -1)
        {
            if (next == '\n') return line.ToString().TrimEnd('\r');
            if (line.Length >= 131072) throw new InvalidDataException("Mensaje LAN demasiado largo");
            line.Append((char)next);
        }
        return line.Length == 0 ? null : line.ToString();
    }

    static string LocalIPv4()
    {
        try
        {
            var addresses = new StringBuilder();
            foreach (var address in Dns.GetHostAddresses(Dns.GetHostName()))
                if (IsLanAddress(address) && !IPAddress.IsLoopback(address))
                {
                    if (addresses.Length > 0) addresses.Append(" / ");
                    addresses.Append(address);
                }
            if (addresses.Length > 0) return addresses.ToString();
        }
        catch (SocketException) { }
        return "consulta la IPv4 de esta computadora";
    }

    static bool IsLanAddress(IPAddress address)
    {
        if (address == null || address.AddressFamily != AddressFamily.InterNetwork) return false;
        byte[] b = address.GetAddressBytes();
        return b[0] == 10 || b[0] == 127 || b[0] == 192 && b[1] == 168 ||
               b[0] == 100 && b[1] >= 64 && b[1] <= 127 ||
               b[0] == 172 && b[1] >= 16 && b[1] <= 31 || b[0] == 169 && b[1] == 254;
    }

    void Receive(string line)
    {
        var parts = line.Split('|');
        if (parts.Length == 0) return;
        if (parts[0] == "HELLO" && parts.Length == 3)
        {
            if (parts[1] != ProtocolVersion)
            {
                Stop();
                Status = "Las dos instalaciones usan versiones LAN incompatibles";
                return;
            }
            PeerCharacter = parts[2];
            Status = "Conectados: elijan personaje y confirmen";
        }
        else if (parts[0] == "READY" && parts.Length == 2)
            PeerReady = parts[1] == "1";
        else if (parts[0] == "CONFIG" && parts.Length == 3 && Role == Seat.Guest &&
                 int.TryParse(parts[1], out int map) && map >= 0 && map <= 2 &&
                 int.TryParse(parts[2], out int tier) && tier >= 1 && tier <= 3)
        {
            var gm = GameManager.Instance;
            if (gm != null) { gm.selectedMap = (GameManager.MapType)map; gm.selectedTier = (GameManager.DifficultyTier)tier; }
            SetReady(false);
        }
        else if (parts[0] == "START" && parts.Length == 4 && Role == Seat.Guest && LocalReady && PeerReady &&
                 int.TryParse(parts[1], out int seed) && int.TryParse(parts[2], out int startMap) &&
                 int.TryParse(parts[3], out int startTier) && startMap >= 0 && startMap <= 2 && startTier >= 1 && startTier <= 3)
        {
            var gm = GameManager.Instance;
            if (gm == null) return;
            gm.selectedMap = (GameManager.MapType)startMap;
            gm.selectedTier = (GameManager.DifficultyTier)startTier;
            CoopRun.Begin(seed);
        }
        else if (parts[0].StartsWith("RUN_", StringComparison.Ordinal)) RunMessage?.Invoke(parts);
    }

    public void StartTogether()
    {
        if (Role != Seat.Host || !Connected || !LocalReady || !PeerReady || CoopRun.Active) return;
        int seed = UnityEngine.Random.Range(1, int.MaxValue);
        var gm = GameManager.Instance;
        if (gm == null) return;
        Send("START", seed.ToString(), ((int)gm.selectedMap).ToString(), ((int)gm.selectedTier).ToString());
        CoopRun.Begin(seed);
    }

    public void SendRun(params string[] fields)
    {
        if (fields.Length == 0 || !fields[0].StartsWith("RUN_", StringComparison.Ordinal)) return;
        Send(fields);
    }

    public void CharacterChanged()
    {
        SetReady(false);
        Send("HELLO", ProtocolVersion, LocalCharacter());
    }

    public void ConfigurationChanged()
    {
        if (Role != Seat.Host) return;
        SetReady(false);
        PeerReady = false;
        SendConfiguration();
    }

    void SendConfiguration()
    {
        var gm = GameManager.Instance;
        if (gm != null) Send("CONFIG", ((int)gm.selectedMap).ToString(), ((int)gm.selectedTier).ToString());
    }

    public void SetReady(bool ready)
    {
        LocalReady = Connected && ready;
        Send("READY", LocalReady ? "1" : "0");
    }

    static string LocalCharacter()
    {
        var gm = GameManager.Instance;
        if (gm == null) return "CL4NK";
        return OriginalContentCatalog.Selected?.displayName ?? gm.selectedCharacter.ToString();
    }

    void Send(params string[] fields)
    {
        if (!Connected || writer == null) return;
        try
        {
            for (int i = 0; i < fields.Length; i++) fields[i] = fields[i].Replace('|', ' ').Replace('\r', ' ').Replace('\n', ' ');
            lock (writeLock) writer.WriteLine(string.Join("|", fields));
        }
        catch (Exception) { LostPeer(generation); }
    }

    void LostPeer(int run)
    {
        if (run != generation) return;
        Stop();
        Status = "El compañero se desconectó. Puedes volver al menú.";
    }

    void Fail(int run, Exception ex)
    {
        if (run != generation) return;
        Stop();
        Status = "No se pudo conectar: " + ex.Message;
    }

    public void Stop()
    {
        Interlocked.Increment(ref generation);
        Role = Seat.Offline;
        Connected = LocalReady = PeerReady = false;
        PeerCharacter = "";
        HostAddress = "";
        try { listener?.Stop(); } catch (SocketException) { }
        try { socket?.Close(); } catch (SocketException) { }
        listener = null;
        socket = null;
        writer = null;
        Status = "Sin conexión LAN";
    }
}
