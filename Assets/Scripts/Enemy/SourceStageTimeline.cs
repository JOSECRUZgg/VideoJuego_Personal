using UnityEngine;

// Times transcribed from StageForest1-3.asset and StageDesert1-3.asset.
// Enemy-card composition is not reconstructed because the exported summoner methods are empty.
public static class SourceStageTimeline
{
    public struct Event { public float start,duration; public bool miniboss; public Event(float minute,float seconds,bool boss){start=minute*60;duration=seconds;miniboss=boss;} }
    static readonly Event[][] Forest={
        new[]{new Event(3,30,true),new Event(4,30,false),new Event(7,30,false),new Event(8,30,true)},
        new[]{new Event(2,15,true),new Event(3,30,false),new Event(6,30,false),new Event(7,45,true)},
        new[]{new Event(1.5f,30,true),new Event(2.5f,30,false),new Event(4,30,false),new Event(5,45,true)}
    };
    static readonly Event[][] Desert={
        new[]{new Event(3,30,true),new Event(4,30,false),new Event(7,30,false),new Event(8,60,true)},
        new[]{new Event(2,30,true),new Event(3,30,false),new Event(6,30,false),new Event(7,30,true)},
        new[]{new Event(1.5f,30,true),new Event(2.5f,30,false),new Event(4,30,false),new Event(5,30,true)}
    };
    public static float StageSeconds(int stage)=>stage<=1?600:stage==2?540:480;
    public static Event[] Events(int stage,bool desert)=>(desert?Desert:Forest)[Mathf.Clamp(stage-1,0,2)];
}
