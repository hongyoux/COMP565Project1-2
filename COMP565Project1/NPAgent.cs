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

            GenerateSensors();

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
            Vector3 sphereForward = agentObject.Forward * (boundingSphereRadius * 2);
            Vector3 sphereLeft = agentObject.Left * (boundingSphereRadius);
            Vector3 sphereRight = agentObject.Right * (boundingSphereRadius);

            left.Center = sphereCenter + sphereForward + sphereLeft;
            right.Center = sphereCenter + sphereForward + sphereRight;
        }

        private void GenerateSensors()
        {
            left = new BoundingSphere();
            right = new BoundingSphere();

            left.Radius = agentObject.ObjectBoundingSphereRadius;
            right.Radius = agentObject.ObjectBoundingSphereRadius;

            UpdateSensors();
        }

        /// <summary>
        /// State machine run that is self-contained and controls object avoidance using sensors.
        /// </summary>
        private void ObjectAvoidance()
        {

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

            }
        }
        public void FindTreasure()
        {
            state = NPAgentState.TREASURE_SEEK;
        }
    }
}
