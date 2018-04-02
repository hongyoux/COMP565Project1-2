/*  
    Copyright (C) 2017 G. Michael Barnes
 
    The file Agent.cs is part of AGMGSKv9 a port and update of AGXNASKv8 from
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
    /// A model that moves.  
    /// Has three Cameras:  first, follow, above.
    /// Camera agentCamera references the currently used camera {first, follow, above}
    /// Follow camera shows the MovableMesh from behind and up.
    /// Above camera looks down on avatar.
    /// The agentCamera (active camera) is updated by the avatar's Update().
    /// 
    /// 1/25/2012 last changed
    /// </summary>
    public abstract class Agent : MovableModel3D
    {
        protected Object3D agentObject = null;
        protected Camera agentCamera, first, follow, above;
        public enum CameraCase { FirstCamera, FollowCamera, AboveCamera }

        protected int treasuresFound = 0;

        /// <summary>
        /// Create an Agent.
        /// All Agents are collidable and have a single instance Object3D named agentObject.
        /// Set StepSize, create first, follow and above cameras.
        /// Set first as agentCamera
        /// <param name="stage"></param>
        /// <param name="label"></param>
        /// <param name="position"></param>
        /// <param name="orientAxis"></param>
        /// <param name="radians"></param>
        /// <param name="meshFile"></param>
        /// </summary>
        public Agent(Stage stage, string label, Vector3 position, Vector3 orientAxis,
           float radians, string meshFile)
           : base(stage, label, meshFile)
        {
            // create an Object3D for this agent
            agentObject = addObject(position, orientAxis, radians);
            first = new Camera(stage, agentObject, Camera.CameraEnum.FirstCamera);
            follow = new Camera(stage, agentObject, Camera.CameraEnum.FollowCamera);
            above = new Camera(stage, agentObject, Camera.CameraEnum.AboveCamera);
            stage.addCamera(first);
            stage.addCamera(follow);
            stage.addCamera(above);
            agentCamera = first;
        }

        // Properties  

        public Object3D AgentObject
        {
            get { return agentObject; }
        }

        public Camera AvatarCamera
        {
            get { return agentCamera; }
            set { agentCamera = value; }
        }

        public Camera Follow
        {
            get { return follow; }
        }

        public Camera Above
        {
            get { return above; }
        }

        public int TreasuresFound
        {
            get { return treasuresFound; }
        }

        // Methods
        public override string ToString()
        {
            return agentObject.Name;
        }

        public void updateCamera()
        {
            agentCamera.updateViewMatrix();
        }

        public override void Update(GameTime gameTime)
        {
            agentObject.updateMovableObject();
            base.Update(gameTime);
            // Agent is in correct (X,Z) position on the terrain 
            // set height to be on terrain -- this is a crude "first approximation" solution.
            // suggest you design and implement your own version either w/in Agent or Stage
            if (isLerpActive)
            {
                lerpHeight();
            }
            else
            {
                stage.setSurfaceHeight(agentObject);
            }
        }

        private void lerpHeight()
        {
            // For ease of understanding
            float currLocX = agentObject.Translation.X;
            float currLocZ = agentObject.Translation.Z;

            //By casting value to int, dividing by spacing and then multiplying by the spacing
            //Will truncate and round down to the nearest aligned spacing. in this case,
            //will truncate to nearest 150 value for X and Z

            //Rest of this algo is taken from slides
            float xA = (int)(currLocX / stage.Spacing) * stage.Spacing;
            float zA = (int)(currLocZ / stage.Spacing) * stage.Spacing;

            Vector3 A = new Vector3(xA, stage.surfaceHeight(xA, zA), zA);
            Vector3 B = new Vector3(xA + 150, stage.surfaceHeight(xA + 150, zA), zA);
            Vector3 C = new Vector3(xA, stage.surfaceHeight(xA, zA + 150), zA + 150);

            float xY = Vector3.Lerp(A, B, ((float)(currLocX - A.X) / stage.Spacing)).Y - A.Y;
            float zY = Vector3.Lerp(A, B, ((float)(currLocZ - A.Z) / stage.Spacing)).Y - A.Y;

            agentObject.Translation = new Vector3(currLocX, A.Y + xY + zY, currLocZ);
        }

    }
}
