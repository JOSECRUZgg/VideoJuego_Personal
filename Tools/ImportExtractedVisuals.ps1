$ErrorActionPreference = 'Stop'
$source = 'C:\Users\JOSEC\Desktop\MATERIAS UT\10MO CUATRIMESTRE\JUAN\MEGABONK\ExportedProject\Assets'
$destination = Join-Path (Split-Path $PSScriptRoot -Parent) 'Assets\Resources\ExtractedVisuals'
$folders = @('GameObject','Mesh','Material','Texture2D','AnimationClip','AnimatorController','AudioClip','Sprite','Font')
$files = @($folders | ForEach-Object { Get-ChildItem -LiteralPath (Join-Path $source $_) -File -Recurse -Force })
$guidMap = @{}
$hash = [System.Security.Cryptography.MD5]::Create()
foreach ($file in $files | Where-Object Extension -eq '.meta') {
    $match = [regex]::Match([IO.File]::ReadAllText($file.FullName), '(?m)^guid: ([a-f0-9]{32})')
    if ($match.Success) {
        $original = $match.Groups[1].Value
        $guidMap[$original] = ([BitConverter]::ToString($hash.ComputeHash([Text.Encoding]::UTF8.GetBytes("UnityShooter.Visuals.$original")))).Replace('-','').ToLowerInvariant()
    }
}
$hash.Dispose()
$utf8 = New-Object Text.UTF8Encoding($false)
$count = 0
foreach ($file in $files) {
    $relative = $file.FullName.Substring($source.Length + 1)
    $target = Join-Path $destination $relative
    [IO.Directory]::CreateDirectory([IO.Path]::GetDirectoryName($target)) | Out-Null
    if ([IO.File]::Exists($target)) {
        $attributes = [IO.File]::GetAttributes($target)
        [IO.File]::SetAttributes($target, ($attributes -band (-bnot [IO.FileAttributes]::Hidden)))
    }
    if ($file.Extension -in @('.png','.ogg','.ttf','.jpg','.webm')) {
        [IO.File]::Copy($file.FullName, $target, $true)
        $count++
        continue
    }
    $text = [IO.File]::ReadAllText($file.FullName)
    $text = [regex]::Replace($text, 'guid: ([a-f0-9]{32})', {
        param($m)
        if ($guidMap.ContainsKey($m.Groups[1].Value)) { 'guid: ' + $guidMap[$m.Groups[1].Value] } else { $m.Value }
    })
    if ($file.Extension -eq '.prefab') {
        # Import visual components only; gameplay belongs to this project's scripts.
        $blocks = [regex]::Split($text, '(?m)(?=^--- !u!)')
        $removed = @{}
        $kept = New-Object 'Collections.Generic.List[string]'
        foreach ($block in $blocks) {
            $header = [regex]::Match($block, '^--- !u!(\d+) &(-?\d+)')
            if ($header.Success -and [int]$header.Groups[1].Value -notin @(1,4,23,33,43,95,120,137,198,199,96)) {
                $removed[$header.Groups[2].Value] = $true
            } else { $kept.Add($block) }
        }
        $text = $kept -join ''
        $text = [regex]::Replace($text, '(?m)^  - component: \{fileID: (-?\d+)\}\r?\n', {
            param($m)
            if ($removed.ContainsKey($m.Groups[1].Value)) { '' } else { $m.Value }
        })
        $text = [regex]::Replace($text, '(?m)^  m_TagString:.*', '  m_TagString: Untagged')
        $text = [regex]::Replace($text, '(?m)^  m_Layer:.*', '  m_Layer: 0')
        $text = [regex]::Replace($text, '(?m)^  m_ApplyRootMotion:.*', '  m_ApplyRootMotion: 0')
    }
    if ($file.Extension -eq '.anim') {
        $text = [regex]::Replace($text, '(?ms)^  m_Events:.*\z', "  m_Events: []`n")
    }
    if ($file.Extension -eq '.mat') {
        # Extracted shaders are compiled game shaders; migrate their albedo to URP.
        $texture = [regex]::Match($text, '(?:_BaseMap|_MainTex|_BaseTexture):\s+m_Texture: (\{fileID: 2800000, guid: [a-f0-9]+, type: \d+\})')
        $texRef = if ($texture.Success) { $texture.Groups[1].Value } else { '{fileID: 0}' }
        $color = [regex]::Match($text, '(?:_BaseColor|_Color): (\{r: [^\r\n]+\})')
        $tint = if ($color.Success) { $color.Groups[1].Value } else { '{r: 1, g: 1, b: 1, a: 1}' }
        $text = "%YAML 1.1`n%TAG !u! tag:unity3d.com,2011:`n--- !u!21 &2100000`nMaterial:`n  serializedVersion: 8`n  m_Name: $($file.BaseName)`n  m_Shader: {fileID: 4800000, guid: 933532a4fcc9baf4fa0491de14d08ed7, type: 3}`n  m_Parent: {fileID: 0}`n  m_ValidKeywords: [ _ALPHATEST_ON ]`n  m_EnableInstancingVariants: 1`n  m_CustomRenderQueue: 2450`n  stringTagMap: {RenderType: TransparentCutout}`n  m_SavedProperties:`n    serializedVersion: 3`n    m_TexEnvs:`n    - _BaseMap:`n        m_Texture: $texRef`n        m_Scale: {x: 1, y: 1}`n        m_Offset: {x: 0, y: 0}`n    m_Ints: []`n    m_Floats:`n    - _AlphaClip: 1`n    - _Cutoff: 0.1`n    - _Cull: 0`n    - _Smoothness: 0`n    - _ZWrite: 1`n    - _SrcBlend: 1`n    - _DstBlend: 0`n    m_Colors:`n    - _BaseColor: $tint`n"
    }
    [IO.File]::WriteAllText($target, $text, $utf8)
    $count++
}
Write-Output "Imported $count visual/audio files and metadata into $destination"
