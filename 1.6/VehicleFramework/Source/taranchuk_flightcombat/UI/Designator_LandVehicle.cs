using RimWorld;
using UnityEngine;
using Vehicles;
using Verse;
using Verse.Sound;

namespace taranchuk_flightcombat
{
    public class Designator_LandVehicle : Designator
    {
        public CompFlightMode comp;
        protected Rot4 placingRot = Rot4.North;

        public Designator_LandVehicle(CompFlightMode comp)
        {
            this.comp = comp;
            this.soundDragSustain = SoundDefOf.Designate_DragBuilding;
            this.soundDragChanged = null;
            this.soundSucceeded = SoundDefOf.Designate_PlaceBuilding;
            this.useMouseIcon = true;
            this.icon = comp.parent.def.uiIcon;
        }

        public override string Label => "CVN_Land".Translate();
        public override string Desc => "CVN_LandDesc".Translate();
        public override Color IconDrawColor => Color.white;
        public override bool Visible => comp.InAir;

        public override void Selected()
        {
            base.Selected();
            Find.MainTabsRoot.EscapeCurrentTab(false);
            placingRot = comp.Vehicle.Rotation;
            comp.LogAlways(() => "Designator_LandVehicle", () => $"Selected. placingRot: {placingRot}, vehicle: {comp.Vehicle}");
        }

        public override void ProcessInput(Event ev)
        {
            if (!CheckCanInteract())
                return;

            base.ProcessInput(ev);
        }

        public override AcceptanceReport CanDesignateCell(IntVec3 c)
        {
            if (!c.InBounds(Map))
                return false;

            if (!comp.Vehicle.Drivable(c))
            {
                return "CVN_CannotLand".Translate();
            }

            var runwayCells = comp.GetLandingRunwayCells(c, placingRot);
            var blockingCells = comp.GetBlockingCells(runwayCells);

            if (blockingCells.Any())
            {
                return "CVN_CannotLand_Obstacles".Translate();
            }

            return true;
        }

        public override void DesignateSingleCell(IntVec3 c)
        {
            comp.LogAlways(() => "Designator_LandVehicle", () => $"Designating landing at {c}, rotation: {placingRot}");
            comp.OrderLanding(c, placingRot);
            Find.DesignatorManager.Deselect();
        }

        public override void SelectedUpdate()
        {
            GenDraw.DrawNoBuildEdgeLines();

            var mouseCell = UI.MouseCell();
            if (mouseCell.InBounds(Map))
            {
                DrawGhost(mouseCell);

                var runwayCells = comp.GetLandingRunwayCells(mouseCell, placingRot);
                var blockingCells = comp.GetBlockingCells(runwayCells);

                GenDraw.DrawFieldEdges(runwayCells, Color.white);
                if (blockingCells.Count > 0)
                {
                    GenDraw.DrawFieldEdges(blockingCells, Color.red);
                }
            }
        }

        public override void DoExtraGuiControls(float leftX, float bottomY)
        {
            DesignatorUtility.GUIDoRotationControls(leftX, bottomY, placingRot, delegate (Rot4 rot)
            {
                placingRot = rot;
                SoundDefOf.DragSlider.PlayOneShotOnCamera();
            });
        }

        protected void DrawGhost(IntVec3 cell)
        {
            Graphic baseGraphic = comp.Vehicle.Graphic;
            Color ghostCol = CanDesignateCell(cell).Accepted ? Designator_Place.CanPlaceColor : Designator_Place.CannotPlaceColor;
            GhostDrawer.DrawGhostThing(cell, placingRot, comp.Vehicle.def, baseGraphic, ghostCol, AltitudeLayer.Blueprint);
        }

        public override void SelectedProcessInput(Event ev)
        {
            if (ev.button == 2)
            {
                 placingRot.Rotate(RotationDirection.Clockwise);
                 SoundDefOf.DragSlider.PlayOneShotOnCamera();
            }

            RotationDirection rotationDirection = RotationDirection.None;
            if (KeyBindingDefOf.Designator_RotateRight.KeyDownEvent)
            {
                rotationDirection = RotationDirection.Clockwise;
            }
            if (KeyBindingDefOf.Designator_RotateLeft.KeyDownEvent)
            {
                rotationDirection = RotationDirection.Counterclockwise;
            }

            if (rotationDirection != RotationDirection.None)
            {
                SoundDefOf.DragSlider.PlayOneShotOnCamera();
                placingRot.Rotate(rotationDirection);
            }

            base.SelectedProcessInput(ev);
        }
    }
}
