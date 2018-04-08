/*  
    Copyright (C) 2017 G. Michael Barnes
 
    The file NPAgent.cs is part of AGMGSKv9 a port and update of AGXNASKv8 from
    MonoGames 3.5 to MonoGames 3.6  

    AGMGSKv9 is free software: you can redistribute it and/or modify
    it under the terms of the GNU General Public License as published by
    the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU General Public License for more details.

    You should have received a copy of the GNU General Public License
    along with this program.  If not, see <http://www.gnu.org/licenses/>.
*/


#region Using Statements
using System;
using System.IO;  // needed for trace()'s fout
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
#endregion

namespace AGMGSKv9
{
    public enum NPAgentState
    {
        TREASURE_SEEK,
        PATH_SEEK,
        DONE
    }

    public enum ObjectAvoidanceState
    {
        GO_FORWARD,
        BACK_UP,

        TURN_RIGHT,
        FOLLOW_WALL_LEFT,
        LEFT_CORNER,
        CORNER_PASS_LEFT,

        TURN_LEFT,
        FOLLOW_WALL_RIGHT,
        RIGHT_CORNER,
        CORNER_PASS_RIGHT
    }

    /// <summary>
    /// A non-playing character that moves.  Override the inherited Update(GameTime)
    /// to implement a movement (strategy?) algorithm.
    /// Distribution NPAgent moves along an "exploration" path that is created by the
    /// from int[,] pathNode array.  The exploration path is traversed in a reverse path loop.
    /// Paths can also be specified in text files of Vector3 values, see alternate
    /// Path class constructors.
    /// 
    /// 1/20/2016 last changed
    /// </summary>
    public class NPAgent : Agent
    {
        private static float kMinDistToTreasure = 4000.0f;
        private NPAgentState state;
        private NavNode nextGoal;
        private Path path;

        private NavNode treasureNav = null;
        private int tagDistance = 200;

        private BoundingSphere left;
        private BoundingSphere right;
        private ObjectAvoidanceState avoidState;
        private int stateNSteps;
        private bool goalVisible;
        private bool leftSensorHit;
        private bool rightSensorHit;
        private bool wallAhead;
        private int breakOut;
        private bool backedUp;
        private bool InWall;

        private int snapDistance = 20;  // this should be a function of step and stepSize
                                        // If using makePath(int[,]) set WayPoint (x, z) vertex positions in the following array
        private int[,] pathNode = { {505, 490}, {500, 500}, {490, 505},  // bottom, right
								    {285, 505}, {275, 500}, {270, 490},  // bottom, middle *TWEAKED*
									{270, 250}, {275, 240}, {285, 235},  // middle, middle *TWEAKED*
                                    {490, 250}, {500, 240}, {505, 235},  // middle, right  *TWEAKED*
									{505, 105}, {500,  95}, {490,  90},  // top, right
                                    {305, 155}, {300, 180}, {295, 155},  // center of map *ADDITION*
                                    {110,  90}, {100,  95}, { 95, 105},  // top, left
									{ 95, 480}, {100, 490}, {110, 495},  // bottom, left
									{445, 460} };                        // loop return

        /// <summary>
        /// Create a NPC. 
        /// AGXNASK distribution has npAgent move following a Path.
        /// </summary>
        /// <param name="theStage"> the world</param>
        /// <param name="label"> name of </param>
        /// <param name="pos"> initial position </param>
        /// <param name="orientAxis"> initial rotation axis</param>
        /// <param name="radians"> initial rotation</param>
        /// <param name="meshFile"> Direct X *.x Model in Contents directory </param>
        public NPAgent(Stage theStage, string label, Vector3 pos, Vector3 orientAxis,
           float radians, string meshFile)
           : base(theStage, label, pos, orientAxis, radians, meshFile)
        {  // change names for on-screen display of current camera
            IsCollidable = true;  // players test collision with Collidable set.
            stage.Collidable.Add(agentObject);  // player's agentObject can be collided with by others.

            InitSensors();

            state = NPAgentState.PATH_SEEK;
            first.Name = "npFirst";
            follow.Name = "npFollow";
            above.Name = "npAbove";

            // Flip a coin. If 0, Go Normal Path. If 1, reverse order of list (Go backwards)
            Random r = new Random();
            int coinFlip = r.Next(2);

            if (coinFlip == 0)
            {
                stage.setInfo(17, "Direction: Regular");
                path = new Path(stage, pathNode, Path.PathType.LOOP); // continuous search path
            }
            else
            {
                stage.setInfo(17, "Direction: Backwards");
                path = new Path(stage, pathNode, Path.PathType.BACKWARDS); // continuous search path backwards *ADDITION
            }

            // path is built to work on specific terrain, make from int[x,z] array pathNode
            stage.Components.Add(path);
            nextGoal = path.NextNode;  // get first path goal
            agentObject.turnToFace(nextGoal.Translation);  // orient towards the first path goal
                                                           // set snapDistance to be a little larger than step * stepSize
            snapDistance = (int)(1.5 * (agentObject.Step * agentObject.StepSize));
        }

        /// <summary>
        /// Simple path following.  If within "snap distance" of a the nextGoal (a NavNode) 
        /// move to the NavNode, get a new nextGoal, turnToFace() that goal.  Otherwise 
        /// continue making steps towards the nextGoal.
        /// </summary>
        public override void Update(GameTime gameTime)
        {
            if (state == NPAgentState.DONE)
            {
                //We're done. stop doing anything.
                return;
            }

            // Part 8 : If within 4000 units of a treasure, autoseek treasure
            Treasure closestTreasure = stage.GetClosestTreasure(agentObject.Translation);
            float closestDistance = float.MaxValue;
            if (closestTreasure != null)
            {
                closestDistance = Vector3.Distance(closestTreasure.Obj.Translation, agentObject.Translation);
            }

            if (state == NPAgentState.PATH_SEEK && closestDistance > kMinDistToTreasure)
            {
                stage.setInfo(21, "Treasure Seeking mode is off");

                PathSeekUpdate(gameTime);
            }
            else
            {
                stage.setInfo(21, "Treasure Seeking mode is on");

                TreasureSeekUpdate(gameTime);
            }

            base.Update(gameTime);  // Agent's Update();
            UpdateSensors();
        }

        private void PathSeekUpdate(GameTime gameTime)
        {
            ObjectAvoidance();

            float distance = Vector3.Distance(
                new Vector3(nextGoal.Translation.X, 0, nextGoal.Translation.Z),
                new Vector3(agentObject.Translation.X, 0, agentObject.Translation.Z));
            stage.setInfo(15, stage.agentLocation(this));
            stage.setInfo(16,
                  string.Format("          nextGoal ({0:f0}, {1:f0}, {2:f0})  distance to next goal = {3,5:f2})",
                      nextGoal.Translation.X / stage.Spacing, nextGoal.Translation.Y, nextGoal.Translation.Z / stage.Spacing, distance));
            if (distance <= snapDistance)
            {
                // snap to nextGoal and orient toward the new nextGoal 
                nextGoal = path.NextNode;
                agentObject.turnToFace(nextGoal.Translation);
            }
        }

        private void TreasureSeekUpdate(GameTime gameTime)
        {
            Treasure t = stage.GetClosestTreasure(agentObject.Translation);
            if (t == null)
            {
                // No more treasure. Stop Moving
                state = NPAgentState.DONE;
                return;
            }

            if (treasureNav == null || treasureNav.Translation != t.Obj.Translation)
            {
                treasureNav = new NavNode(t.Obj.Translation, NavNode.NavNodeEnum.WAYPOINT);
                agentObject.turnToFace(treasureNav.Translation);
            }

            ObjectAvoidance();

            float distance = Vector3.Distance(
                new Vector3(treasureNav.Translation.X, 0, treasureNav.Translation.Z),
                new Vector3(agentObject.Translation.X, 0, agentObject.Translation.Z));
            stage.setInfo(15, stage.agentLocation(this));
            stage.setInfo(16,
                  string.Format("          nextGoal ({0:f0}, {1:f0}, {2:f0})  distance to next goal = {3,5:f2})",
                      treasureNav.Translation.X / stage.Spacing, treasureNav.Translation.Y, treasureNav.Translation.Z / stage.Spacing, distance));
            // Add in the bounding sphere radius to make distance 200 from edge of bounding box instead of 200 from center of object
            if (distance <= (tagDistance + t.BoundingSphereRadius))
            {
                //Tag the treasure so it can't be found again.
                stage.TagTreasure(t, this);
                this.treasuresFound++;
                //Found treasure, stop seeking and switch states.
                treasureNav = null;

                Treasure anyMore = stage.GetClosestTreasure(agentObject.Translation);
                if (anyMore != null)
                {
                    state = NPAgentState.PATH_SEEK;
                    // snap to nextGoal and orient toward the new nextGoal 
                    agentObject.turnToFace(nextGoal.Translation);
                }
                else
                {
                    state = NPAgentState.DONE;
                }
            }

        }

        private void UpdateSensors()
        {
            float boundingSphereRadius = agentObject.ObjectBoundingSphereRadius;
            Vector3 sphereCenter = agentObject.Translation;
            Vector3 sphereForward = agentObject.Forward * (boundingSphereRadius * 2f);
            Vector3 sphereLeft = agentObject.Left * (boundingSphereRadius * .5f);
            Vector3 sphereRight = agentObject.Right * (boundingSphereRadius * .5f);

            left.Center = sphereCenter + sphereForward + sphereLeft;
            right.Center = sphereCenter + sphereForward + sphereRight;

        }

        private void InitSensors()
        {
            left = new BoundingSphere();
            right = new BoundingSphere();

            left.Radius = agentObject.ObjectBoundingSphereRadius;
            right.Radius = agentObject.ObjectBoundingSphereRadius;

            UpdateSensors();

            avoidState = ObjectAvoidanceState.GO_FORWARD;
            stateNSteps = 0;
            goalVisible = true;
            leftSensorHit = false;
            rightSensorHit = false;
            wallAhead = false;
        }

        /// <summary>
        /// State machine run that is self-contained and controls object avoidance using sensors.
        /// </summary>
        private void ObjectAvoidance()
        {
            stage.setInfo(26, String.Format("Avoidance State: {0}", avoidState.ToString()));
            // Move in the current direction for n steps
            if (stateNSteps != 0)
            {
                stateNSteps--;
                return;
            }

            CheckSensors();

            if (InWall)
            {
                avoidState = ObjectAvoidanceState.BACK_UP;
            }

            switch(avoidState)
            {
                default:
                case ObjectAvoidanceState.GO_FORWARD:
                    {
                        if (goalVisible)
                        {
                            Vector3 goalLocation = (treasureNav != null) ? treasureNav.Translation : nextGoal.Translation;
                            agentObject.turnToFace(goalLocation);
                        }

                        if (leftSensorHit && wallAhead)
                        {
                            agentObject.Step = 0;
                            avoidState = ObjectAvoidanceState.TURN_RIGHT;
                        }
                        else if (rightSensorHit && wallAhead)
                        {
                            agentObject.Step = 0;
                            avoidState = ObjectAvoidanceState.TURN_LEFT;
                        }
                        break;
                    }
                case ObjectAvoidanceState.TURN_LEFT:
                    {
                        if (rightSensorHit)
                        {
                            //Stop moving forward and turn right until left sensor is not touching the wall
                            agentObject.Orientation = Matrix.CreateRotationY((float)(Math.PI / 16)) * agentObject.Orientation;
                            agentObject.Step = 0;
                        }
                        else
                        {
                            //Turn to the left once sensor separates so that we can stay along the wall
                            agentObject.Orientation = Matrix.CreateRotationY((float)(-Math.PI / 16)) * agentObject.Orientation;
                            agentObject.Step = 1;
                            avoidState = ObjectAvoidanceState.FOLLOW_WALL_RIGHT;
                        }

                        break;
                    }
                case ObjectAvoidanceState.TURN_RIGHT:
                    {
                        if (leftSensorHit)
                        {
                            //Stop moving forward and turn right until left sensor is not touching the wall
                            agentObject.Orientation = Matrix.CreateRotationY((float)(-Math.PI / 16)) * agentObject.Orientation;
                            agentObject.Step = 0;
                        }
                        else
                        {
                            //Turn to the left once sensor separates so that we can stay along the wall
                            agentObject.Orientation = Matrix.CreateRotationY((float)(Math.PI / 16)) * agentObject.Orientation;
                            agentObject.Step = 1;
                            avoidState = ObjectAvoidanceState.FOLLOW_WALL_LEFT;
                        }

                        break;
                    }
                case ObjectAvoidanceState.FOLLOW_WALL_LEFT:
                    {
                        //Stay close to the wall but not in it
                        //If agent is angled toward wall still, on wallhit go back to turn right state.
                        //If agent is angled away from wall, turn back towards wall.
                        //If agent was angled so far away from wall that it takes two iterations of turns, that means we're at a corner.
                        if (wallAhead)
                        {
                            avoidState = ObjectAvoidanceState.TURN_RIGHT;
                            breakOut++;
                            if (breakOut > 60)
                            {
                                breakOut = 0;
                                stateNSteps = 120;
                                avoidState = ObjectAvoidanceState.BACK_UP;
                            }
                        }

                        if (!wallAhead && leftSensorHit)
                        {
                            // Walk forward
                        }
                        
                        if (!leftSensorHit)
                        {
                            //At Corner
                            stateNSteps = 60;
                            // Go around the corner
                            avoidState = ObjectAvoidanceState.LEFT_CORNER;
                        }
                        break;
                    }
                case ObjectAvoidanceState.FOLLOW_WALL_RIGHT:
                    {
                        //Stay close to the wall but not in it
                        //If agent is angled toward wall still, on wallhit go back to turn right state.
                        //If agent is angled away from wall, turn back towards wall.
                        //If agent was angled so far away from wall that it takes two iterations of turns, that means we're at a corner.
                        if (wallAhead)
                        {
                            avoidState = ObjectAvoidanceState.TURN_LEFT;
                            breakOut++;
                            if (breakOut > 60)
                            {
                                breakOut = 0;
                                stateNSteps = 120;
                                avoidState = ObjectAvoidanceState.BACK_UP;
                            }
                        }

                        if (!wallAhead && rightSensorHit)
                        {
                            // Walk forward
                        }

                        if (!rightSensorHit)
                        {
                            //At Corner
                            stateNSteps = 60;
                            // Go around the corner
                            avoidState = ObjectAvoidanceState.RIGHT_CORNER;
                        }

                        if (!goalVisible && !wallAhead && !leftSensorHit && !rightSensorHit)
                        {
                            //Can't move towards goal, no walls ahead and no sensor hits means im on the outer edge of map
                            breakOut++;
                            if (breakOut > 60)
                            {
                                //Ran into the outer walls most likely
                                //About face and go straight
                                breakOut = 0;
                                agentObject.Orientation = Matrix.CreateRotationY((float)(Math.PI)) * agentObject.Orientation;
                                avoidState = ObjectAvoidanceState.GO_FORWARD;
                            }
                        }

                        break;
                    }
                case ObjectAvoidanceState.BACK_UP:
                    {
                        if (backedUp == false)
                        {
                            //Move 90 Degrees to the left and walk in that direction
                            agentObject.Orientation = Matrix.CreateRotationY((float)(-Math.PI / 2)) * agentObject.Orientation;
                            stateNSteps = 60;
                            backedUp = true;
                        }
                        else
                        {
                            agentObject.Orientation = Matrix.CreateRotationY((float)(-Math.PI / 2)) * agentObject.Orientation;
                            avoidState = ObjectAvoidanceState.GO_FORWARD;
                        }

                        break;
                    }
                case ObjectAvoidanceState.LEFT_CORNER:
                    {
                        if (!leftSensorHit)
                        {
                            agentObject.Orientation = Matrix.CreateRotationY((float)(Math.PI / 16)) * agentObject.Orientation;

                            breakOut++;
                            if (breakOut > 60)
                            {
                                breakOut = 0;
                                avoidState = ObjectAvoidanceState.GO_FORWARD;
                            }
                        }
                        else
                        {
                            agentObject.Orientation = Matrix.CreateRotationY((float)(-Math.PI / 16)) * agentObject.Orientation;

                            stateNSteps = 60;
                            avoidState = ObjectAvoidanceState.CORNER_PASS_LEFT;
                        }
                        break;
                    }
                case ObjectAvoidanceState.RIGHT_CORNER:
                    {
                        if (!rightSensorHit)
                        {
                            agentObject.Orientation = Matrix.CreateRotationY((float)(-Math.PI / 16)) * agentObject.Orientation;

                            breakOut++;
                            if (breakOut > 60)
                            {
                                avoidState = ObjectAvoidanceState.GO_FORWARD;
                            }
                        }
                        else
                        {
                            agentObject.Orientation = Matrix.CreateRotationY((float)(Math.PI / 16)) * agentObject.Orientation;

                            stateNSteps = 60;
                            avoidState = ObjectAvoidanceState.CORNER_PASS_RIGHT;
                        }
                        break;
                    }
                case ObjectAvoidanceState.CORNER_PASS_LEFT:
                    {
                        if (goalVisible)
                        {
                            Vector3 goalLocation = (treasureNav != null) ? treasureNav.Translation : nextGoal.Translation;
                            agentObject.turnToFace(goalLocation);
                            avoidState = ObjectAvoidanceState.GO_FORWARD;
                            return;
                        }
                        else
                        {
                            breakOut++;
                            avoidState = ObjectAvoidanceState.FOLLOW_WALL_LEFT;
                            if (breakOut > 60)
                            {
                                //Ran into the outer walls most likely
                                //About face and go straight
                                breakOut = 0;
                                agentObject.Orientation = Matrix.CreateRotationY((float)(Math.PI)) * agentObject.Orientation;
                                avoidState = ObjectAvoidanceState.GO_FORWARD;
                            }
                        }

                        break;
                    }
                case ObjectAvoidanceState.CORNER_PASS_RIGHT:
                    {
                        if (goalVisible)
                        {
                            Vector3 goalLocation = (treasureNav != null) ? treasureNav.Translation : nextGoal.Translation;
                            agentObject.turnToFace(goalLocation);
                            avoidState = ObjectAvoidanceState.GO_FORWARD;
                            return;
                        }
                        else
                        {
                            breakOut++;
                            avoidState = ObjectAvoidanceState.FOLLOW_WALL_RIGHT;
                            if (breakOut > 60)
                            {
                                //Ran into the outer walls most likely
                                //About face and go straight
                                breakOut = 0;
                                agentObject.Orientation = Matrix.CreateRotationY((float)(Math.PI)) * agentObject.Orientation;
                                avoidState = ObjectAvoidanceState.GO_FORWARD;
                            }
                        }

                        break;
                    }
            }
        }

        private void CheckSensors()
        {
            if ((left.Center.X < 0 || left.Center.X > 76800 || left.Center.Z < 0 || left.Center.Z > 76800) ||
                (right.Center.X < 0 || right.Center.X > 76800 || right.Center.Z < 0 || right.Center.Z > 76800))
            {
                InWall = true;
                return;
            }

            InWall = false;
            leftSensorHit = false;
            rightSensorHit = false;
            wallAhead = false;
            goalVisible = true;

            Vector3 goalLocation = (treasureNav != null) ? treasureNav.Translation : nextGoal.Translation;

            Vector3 yIgnoredGoalLocation = goalLocation;
            yIgnoredGoalLocation.Y = 0;

            Vector3 yIgnoredAgentObjectTranslation = agentObject.Translation;
            yIgnoredAgentObjectTranslation.Y = 0;

            Vector3 nextStepTranslation = yIgnoredAgentObjectTranslation + agentObject.Forward * agentObject.StepSize * 2;
            Vector3 goalTranslation = yIgnoredAgentObjectTranslation + Vector3.Normalize(yIgnoredGoalLocation - yIgnoredAgentObjectTranslation) * agentObject.StepSize * 2;

            foreach (Object3D obj in stage.Collidable)
            {
                if (obj == agentObject)
                {
                    continue;
                }

                Vector3 yIgnoredTranslation = obj.Translation;
                yIgnoredTranslation.Y = 0;

                Vector3 yIgnoredLeft = left.Center;
                yIgnoredLeft.Y = 0;
                Vector3 yIgnoredRight = right.Center;
                yIgnoredRight.Y = 0;


                if (Vector3.Distance(yIgnoredLeft, yIgnoredTranslation) <= (obj.ObjectBoundingSphereRadius + left.Radius))
                {
                    leftSensorHit = true;
                }
                if (Vector3.Distance(yIgnoredRight, yIgnoredTranslation) <= (obj.ObjectBoundingSphereRadius + right.Radius))
                {
                    rightSensorHit = true;
                }
                if (Vector3.Distance(nextStepTranslation, yIgnoredTranslation) <= (obj.ObjectBoundingSphereRadius + agentObject.ObjectBoundingSphereRadius))
                {
                    wallAhead = true;
                }
                if (Vector3.Distance(goalTranslation, yIgnoredTranslation) <= (obj.ObjectBoundingSphereRadius + agentObject.ObjectBoundingSphereRadius))
                {
                    goalVisible = false;
                }

            }
        }

        public List<BoundingSphere> BoundingSpheres
        {
            get
            {
                List<BoundingSphere> sphereList = new List<BoundingSphere>
                {
                    left,
                    right
                };

                return sphereList;
            }
        }
        public override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);
            if (stage.DrawSensors)
            {
                Matrix[] modelTransforms = new Matrix[model.Bones.Count];

                foreach (BoundingSphere b in BoundingSpheres)
                {
                    model.CopyAbsoluteBoneTransformsTo(modelTransforms);

                    Matrix objectBoundingSphereWorld = Matrix.CreateScale(b.Radius * 2);
                    objectBoundingSphereWorld *= Matrix.CreateTranslation(b.Center);

                    foreach (ModelMesh mesh in stage.BoundingSphere3D.Meshes)
                    {
                        foreach (BasicEffect effect in mesh.Effects)
                        {
                            effect.EnableDefaultLighting();
                            if (stage.Fog)
                            {
                                effect.FogColor = Color.CornflowerBlue.ToVector3();
                                effect.FogStart = 50;
                                effect.FogEnd = 500;
                                effect.FogEnabled = true;
                            }
                            else effect.FogEnabled = false;
                            effect.DirectionalLight0.DiffuseColor = stage.DiffuseLight;
                            effect.AmbientLightColor = stage.AmbientLight;
                            effect.DirectionalLight0.Direction = stage.LightDirection;
                            effect.DirectionalLight0.Enabled = true;
                            effect.View = stage.View;
                            effect.Projection = stage.Projection;
                            effect.World = objectBoundingSphereWorld * modelTransforms[mesh.ParentBone.Index];
                        }
                        stage.setBlendingState(true);
                        mesh.Draw();
                        stage.setBlendingState(false);
                    }
                }
            }
        }
        public void FindTreasure()
        {
            state = NPAgentState.TREASURE_SEEK;
        }
    }
}
