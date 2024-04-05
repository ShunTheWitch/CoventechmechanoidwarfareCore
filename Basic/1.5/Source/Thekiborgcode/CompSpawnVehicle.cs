using System.Collections.Generic;
using RimWorld;
using UnityEngine;
using Vehicles;
using Verse;

namespace Thek_VFSpawner
{
    public class CompSpawnVehicle : ThingComp
    {
        Thing thingConsumed;
        ThingDef thingDefConsumed;
        private int cooldownRemaining = 0;
        private Color iconColor = Color.white;

        public CompProperties_SpawnerVehicle Props => (CompProperties_SpawnerVehicle)props;

        public VehiclePawn Vehicle => parent as VehiclePawn;

        public override IEnumerable<Gizmo> CompGetGizmosExtra()
        {
            Command_ActionWithCooldown commandAction = new()
            {
                defaultLabel = "Deploy Vehicle",
                icon = ContentFinder<Texture2D>.Get("Gizmo/SpawnGizmo_Icon", true), //This is the path to the icon, going off the Textures folder, SpawnGizmo_Icon would be the name of the picture.
                action = delegate ()
                {
                    if (cooldownRemaining <= 0)
                    {
                        if (/*Event.current.button == 0 ||*/ Event.current.button == 1)
                        // (Left Mouse Button or) Right Mouse Button, if you click gizmos with the wheel you're sick
                        {
                            DoFloatMenuButtons();
                        }
                    }
                    else
                    {
                        if (Event.current.button == 1)
                        {
                            Messages.Message("Ability is on cooldown.", MessageTypeDefOf.RejectInput, false);
                        }
                    }
                },
                cooldownPercentGetter = () => Mathf.InverseLerp(Props.cooldownTimeInTicks, 0f, cooldownRemaining),
                defaultIconColor = iconColor,
            };
            yield return commandAction;
        }


        /// <summary>
        /// Handles all the logic behind adding buttons to the float menu
        /// </summary>
        private void DoFloatMenuButtons()
        {
            List<FloatMenuOption> options = new(); //List that stores each individual button from a float menu

            foreach (ThingDef thingDef in Props.thingDefsConsumedForVehicle) //Iterates through the def's mod extensions
            {
                if (Vehicle.inventory.innerContainer.NullOrEmpty())
                {
                    options.Add(new FloatMenuOption("Missing item to spawn " + thingDef.GetModExtension<ModExtension_VehicleDefToSpawn>().VehicleDefToSpawn.label + ", " + thingDef.label + " needed.", () => { }, MenuOptionPriority.DisabledOption));
                }
                else if (Vehicle.inventory.innerContainer.Contains(thingDef)) //If it finds the thing we're looking for to consume when spawning the vehicle
                {
                    thingDefConsumed = thingDef;
                    thingConsumed = GetThingFromThingDef(thingDef);
                    options.Add(new FloatMenuOption("Launch " + thingDef.label + ".", Targeter));
                }
                else
                {
                    options.Add(new FloatMenuOption("Missing item to spawn " + thingDef.GetModExtension<ModExtension_VehicleDefToSpawn>().VehicleDefToSpawn.label + ", " + thingDef.label + " needed.", () => { }, MenuOptionPriority.DisabledOption));
                }
            }
            Find.WindowStack.Add(new FloatMenu(options)); //This creates a float menu using the list options to create each individual button
        }


        /// <summary>
        /// Associates the thingdef we get from the vehicle's inventory with it's thing
        /// </summary>
        private Thing GetThingFromThingDef(ThingDef thingDef)
        {
            foreach (Thing ThingWantedFromDef in Vehicle.inventory.innerContainer)
            {
                if (ThingWantedFromDef.def == thingDef)
                {
                    return ThingWantedFromDef;
                }
            }
            return null;
        }


        /// <summary>
        /// Handles targeting a map tile to spawn the vehicle, using the TargetingParameters to target ONLY the ground
        /// </summary>
        private void Targeter()
        {
            var generatedVehicle = VehicleSpawner.GenerateVehicle(thingDefConsumed.GetModExtension<ModExtension_VehicleDefToSpawn>().VehicleDefToSpawn, Faction.OfPlayer);
            //This calls the GenerateVehicle method that generates the vehicle, but it doesn't spawn it yet, for that we use GenSpawn, with the Vehicle we just generated as argument.

            Find.Targeter.BeginTargeting(TargetingParameters,
                action: (LocalTargetInfo target) =>
                {
                    VehiclePawn genVehicle = (VehiclePawn)GenSpawn.Spawn(generatedVehicle, Vehicle.Position, Vehicle.Map);
                    if (this.parent is Pawn pawn)
                    {
                        var overseerSubject = pawn.OverseerSubject;
                        if (overseerSubject != null)
                        {
                            genVehicle.GetOverseer()?.relations.RemoveDirectRelation(PawnRelationDefOf.Overseer, genVehicle);
                            pawn.relations.AddDirectRelation(PawnRelationDefOf.Overseer, genVehicle);
                        }
                    }

                    ///This spawns the vehicle that gets animated, as MakeFlyer requires something to despawn before doing it's thing
                    PawnFlyer droneFlyer = PawnFlyer.MakeFlyer(ThingDefOf.PawnJumper, genVehicle, Vehicle.Position, EffecterDefOf.Interceptor_BlockedProjectile, SoundDefOf.TurretAcquireTarget);
                    //Then, we make the flyer, that will despawn the vehicle spawned before.
                    GenSpawn.Spawn(droneFlyer, target.Cell, Vehicle.Map);
                    //Spawning the Flyer makes the animation start, the vehicle travels from the Launcher's position (due to GenSpawn spawning the 2nd inside the first one), to the cell specified by the targeter. Once it reaches there, the 2nd vehicle gets created.


                    thingConsumed.SplitOff(1).Destroy();
                    //Doing StackCount-- is bad, skips code, SplitOff is better for substracting items from stacks

                    cooldownRemaining = Props.cooldownTimeInTicks;
                    //Cooldown set in the XML

                    iconColor = new(0.62f, 0f, 0f, 1f);
                },


                //This is used to draw a ghost of the vehicle.
                highlightAction: (LocalTargetInfo target) =>
                {
                    GenDraw.DrawRadiusRing(Vehicle.Position, Props.rangeArea);
                    Color colorRed = new(1f, 0f, 0f, 0.4f);

                    if (MapHelper.VehicleBlockedInPosition(generatedVehicle, Vehicle.Map, target.Cell, generatedVehicle.Rotation))
                    //If something blocks the area where you want to spawn the vehicle in
                    {
                        GhostDrawer.DrawGhostThing(target.Cell, generatedVehicle.Rotation, generatedVehicle.def, generatedVehicle.Graphic, colorRed, AltitudeLayer.Blueprint);
                        //The ghost will be drawn red
                    }
                    else
                    {
                        GhostDrawer.DrawGhostThing(target.Cell, generatedVehicle.Rotation, generatedVehicle.def, generatedVehicle.Graphic, Color.white, AltitudeLayer.Blueprint);
                        //If nothing is blocking it it will be drawn white
                    }
                },


                //It required me this, so i just used it for null checking.
                targetValidator: (LocalTargetInfo target) =>
                {
                    if (thingDefConsumed != null && thingConsumed != null)
                    {
                        if ((Vehicle.Position - target.Cell).LengthHorizontalSquared < Mathf.Pow(Props.rangeArea, 2))
                        {
                            if (!MapHelper.VehicleBlockedInPosition(generatedVehicle, Vehicle.Map, target.Cell, generatedVehicle.Rotation))
                            {
                                return true;
                            }

                            else
                            {
                                Messages.Message("Not enough space to launch vehicle", MessageTypeDefOf.RejectInput, false);
                                return false;
                            }
                        }

                        else
                        {
                            Messages.Message("Can't launch vehicle outside of the range", MessageTypeDefOf.RejectInput, false);
                        }
                        return false;
                    }
                    return false;
                },
                caster: Vehicle);
        }


        public override void CompTick()
        //For the gizmo's cooldown
        {
            if (cooldownRemaining > 0)
            {
                cooldownRemaining--; //Honorable mention: Steve, whom i stole the idea from
            }
            else
            {
                iconColor = Color.white;
            }
            base.CompTick();
        }


        /// <summary>
        /// Defines what the Targeter can target to.
        /// </summary>
        private readonly TargetingParameters TargetingParameters = new()
        {
            canTargetPawns = false,
            canTargetBuildings = false,
            canTargetAnimals = false,
            canTargetHumans = false,
            canTargetMechs = false,
            canTargetLocations = true,
        };
    }
}