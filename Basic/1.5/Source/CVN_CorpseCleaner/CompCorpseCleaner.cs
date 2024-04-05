using RimWorld;
using System;
using System.Collections.Generic;
using UnityEngine;
using Verse;
using Vehicles;

namespace CVN_CorpseCleaner
{
    public class CompCorpseCleaner : ThingComp
    {
        public CompProperties_CorpseCleaner Props => (CompProperties_CorpseCleaner)props;

		public int progress = 0;

		public bool isProcessing = false;

		public int corpseAbsorbed = 0;
		public float percentComplete => (int)Math.Round((double)(100 * progress) / (double)Props.timeToComplete);

		public override void CompTick()
        {
            base.CompTick();
			if(isProcessing)
            {
				progress++;
				if(progress == Props.timeToComplete)
                {
					SpawnThing(Props.thingToSpawn, Props.amountToSpawnPerCorpse * corpseAbsorbed);
					corpseAbsorbed = 0;
					progress = 0;
					isProcessing = false;
				}
            }
        }
        public override void PostExposeData()
        {
            base.PostExposeData();
			Scribe_Values.Look(ref isProcessing, "isProcessing", false);
			Scribe_Values.Look(ref progress, "progress", 0);
        }
        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
			Command_Action command_Action = new Command_Action();
			command_Action.defaultLabel = Props.gizmoName;
			command_Action.defaultDesc = Props.gizmoDesc;
			command_Action.icon = ContentFinder<Texture2D>.Get(Props.gizmoIcon);
			command_Action.action = delegate
			{
				foreach(var item in GenRadial.RadialDistinctThingsAround(parent.Position,parent.Map,Props.radius,true))
                {
					if (item is Corpse)
					{
						MoteMovingToPoint moteMovingToPoint = (MoteMovingToPoint)ThingMaker.MakeThing(Props.orbDef);
						moteMovingToPoint.Attach(item, parent);
						moteMovingToPoint.exactPosition = item.DrawPos;
						GenSpawn.Spawn(moteMovingToPoint, item.Position, parent.Map);
						item.Destroy();
						corpseAbsorbed++;
					}
					else
                    {
						continue;
                    }
                }
				if(corpseAbsorbed > 0)
                {
					isProcessing = true;
				}				
			};
			yield return command_Action;
		}
		public void SpawnThing(ThingDef thingDef,int amount)
        {
			Thing newThing = ThingMaker.MakeThing(thingDef);
			newThing.stackCount = amount;
			GenSpawn.Spawn(newThing,parent.Position,parent.Map);
			if(parent is VehiclePawn veh)
            {
				veh.inventory.TryAddAndUnforbid(newThing);
            }
        }
		public override string CompInspectStringExtra()
		{
			string text = "conversion progress: ";
			text += percentComplete + "%"; 
			return text + base.CompInspectStringExtra();
		}
	}
}
