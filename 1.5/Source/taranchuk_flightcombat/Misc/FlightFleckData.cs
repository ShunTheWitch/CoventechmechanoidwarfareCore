using UnityEngine;
using Verse;

namespace taranchuk_flightcombat
{
    public class FlightFleckData
    {
        public FleckDef fleck;
        public Vector3 position;
        public float velocitySpeed;
        public float solidTime;
        public bool solidTimeScaleByTakeoff;
        public bool solidTimeScaleByTakeoffInverse;
        public float angleOffset;
        public int spawnTickRate = 1;
        public float scale = 1f;
        public bool attachToVehicle;
    }
}
