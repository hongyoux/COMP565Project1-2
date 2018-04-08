/*  
    Copyright (C) 2017 G. Michael Barnes
 
    The file Pack.cs is part of AGMGSKv9 a port and update of AGXNASKv8 from
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

    /// <summary>
    /// Pack represents a "flock" of MovableObject3D's Object3Ds.
    /// Usually the "player" is the leader and is set in the Stage's LoadContent().
    /// With no leader, determine a "virtual leader" from the flock's members.
    /// Model3D's inherited List<Object3D> instance holds all members of the pack.
    /// 
    /// 2/1/2016 last changed
    /// </summary>
    public class Pack : MovableModel3D
    {
        Object3D leader;
        /// <summary>
        /// Construct a pack with an Object3D leader
        /// </summary>
        /// <param name="theStage"> the scene </param>
        /// <param name="label"> name of pack</param>
        /// <param name="meshFile"> model of a pack instance</param>
        /// <param name="xPos, zPos">  approximate position of the pack </param>
        /// <param name="aLeader"> alpha dog can be used for flock center and alignment </param>
        public Pack(Stage theStage, string label, string meshFile, int nDogs, int xPos, int zPos, Object3D theLeader)
           : base(theStage, label, meshFile)
        {
            isCollidable = true;
            random = new Random();
            leader = theLeader;
            int spacing = stage.Spacing;
            // initial vertex offset of dogs around (xPos, zPos)
            int[,] position = { { 0, 0 }, { 7, -4 }, { -5, -2 }, { -7, 4 }, { 5, 2 } };
            for (int i = 0; i < position.GetLength(0); i++)
            {
                int x = xPos + position[i, 0];
                int z = zPos + position[i, 1];
                float scale = (float)(0.5 + random.NextDouble());
                addObject(new Vector3(x * spacing, stage.surfaceHeight(x, z), z * spacing),
                              new Vector3(0, 1, 0), 0.0f,
                              new Vector3(scale, scale, scale));
            }
        }

        /// <summary>
        /// Each pack member's orientation matrix will be updated.
        /// Distribution has pack of dogs moving randomly.  
        /// Supports leaderless and leader based "flocking" 
        /// </summary>      
        public override void Update(GameTime gameTime)
        {
            int packingPercentage = random.Next(100);

            if (packingPercentage < stage.PackingAmount * 33)
            {
                PackingUpdate(gameTime);
            }
            else
            {
                RegularUpdate(gameTime);
            }
            base.Update(gameTime);  // MovableMesh's Update();
        }

        private Vector3 GetCohesionForce(Object3D obj)
        {
            Vector3 cohesion = (leader.Translation - obj.Translation);
            cohesion.Normalize();
            return cohesion;
        }

        private Vector3 GetSeparationForce(Object3D obj)
        {
            Vector3 sepForce = new Vector3();
            foreach (Object3D boid in instance)
            {
                if (obj != boid)
                {
                    float distanceSquared = Vector3.DistanceSquared(boid.Translation, obj.Translation);
                    Vector3 transDiff = (boid.Translation - obj.Translation);
                    sepForce += transDiff / distanceSquared;
                }
            }

            return sepForce / instance.Count;
        }

        private Vector3 GetAlignmentForce(Object3D obj)
        {
            Vector3 alignment = leader.Forward;
            alignment.Normalize();
            return alignment;
        }

        private float GetCohesionWeight(Object3D obj)
        {
            float distance = Vector3.Distance(obj.Translation, leader.Translation);
            if (distance < 2000)
            {
                return 0;
            }
            else if (distance >= 3000)
            {
                return 1;
            }
            else
            {
                return 1 - (3000 - distance) / 1000;
            }
        }
        private float GetSeparationWeight(Object3D obj)
        {
            float distance = Vector3.Distance(obj.Translation, leader.Translation);
            if (distance <= 400)
            {
                return 1;
            }
            else if (distance >= 1000)
            {
                return 0;
            }
            else
            {
                return (1000 - distance) / 600;
            }
        }

        private float GetAlignmentWeight(Object3D obj)
        {
            float distance = Vector3.Distance(obj.Translation, leader.Translation);
            if (distance < 400 || distance > 3000)
            {
                return 0;
            }
            else if (distance < 1000)
            {
                return 1 - ((1000 - distance) / 600);
            }
            else if (distance > 2000)
            {
                return (3000 - distance) / 1000;
            }
            else
            {
                return 1;
            }
        }

        private void PackingUpdate(GameTime gameTime)
        {
            foreach (Object3D obj in instance)
            {
                Vector3 cohesion = GetCohesionForce(obj) * GetCohesionWeight(obj);
                Vector3 separation = GetSeparationForce(obj) * GetSeparationWeight(obj);
                Vector3 alignment = GetAlignmentForce(obj) * GetAlignmentWeight(obj);

                Vector3 newDirection = cohesion + separation + alignment;

                Vector3 originalDirection = obj.Forward;
                originalDirection.Normalize();

                // Adjust angle of motion
                newDirection.Normalize();
                double cosAngle = Vector3.Dot(originalDirection, newDirection);

                //If Cos is 1 or -1 or outside that number from rounding / lossy numbers, then the angle is parallel 
                if (!(cosAngle >= 1) && !(cosAngle <= -1))
                {
                    float angle = (float)Math.Acos(cosAngle);
                    if (angle < 0)
                    {
                        angle += (float)(2 * Math.PI);
                    }

                    obj.Yaw = angle;
                }

                obj.updateMovableObject();
                stage.setSurfaceHeight(obj);
            }
        }

        private void RegularUpdate(GameTime gameTime)
        {
            // if (leader == null) need to determine "virtual leader from members"
            float angle = 0.3f;
            foreach (Object3D obj in instance)
            {
                obj.Yaw = 0.0f;
                // change direction 4 time a second  0.07 = 4/60
                if (random.NextDouble() < 0.07)
                {
                    if (random.NextDouble() < 0.5) obj.Yaw -= angle; // turn left
                    else obj.Yaw += angle; // turn right
                }
                obj.updateMovableObject();
                stage.setSurfaceHeight(obj);
            }
        }

        public Object3D Leader
        {
            get { return leader; }
            set { leader = value; }
        }

    }
}
