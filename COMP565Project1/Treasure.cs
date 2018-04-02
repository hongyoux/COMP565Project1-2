/*  
    Hongyou Xiong
    COMP 565 Project 1 Additional files
    Treasure class that holds information about tagged treasures
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
    /// An example of how to override the MovableModel3D's Update(GameTime) to 
    /// animate a model's objects.  The actual update of values is done by calling 
    /// each instance object and setting its (Pitch, Yaw, Roll, or Step property. 
    /// Then call base.Update(GameTime) method of MovableModel3D to apply transformations.
    /// 
    /// 1/5/2014  last changed
    /// </summary>
    public class Treasure : MovableModel3D
    {
        private Object3D treasureObj;
        private Agent taggedAgent = null;

        // Constructor
        public Treasure(Stage stage, string label, string meshFile, int locX, int locZ)
           : base(stage, label, meshFile)
        {
            float scaledX = locX * stage.Spacing;
            float scaledZ = locZ * stage.Spacing;

            random = new Random();

            treasureObj = addObject(
                new Vector3(scaledX, stage.surfaceHeight(scaledX, scaledZ), scaledZ),
                new Vector3(0, 1, 0),
                random.Next(5) * 0.01f);
        }

        public Object3D Obj
        {
            get { return treasureObj; }
        }

        public void Tag(Agent agent)
        {
            taggedAgent = agent;
        }

        public bool IsTagged()
        {
            return taggedAgent != null;
        }

        public String GetTaggedName()
        {
            if (!IsTagged())
            {
                return "NOT FOUND";
            }
            return taggedAgent.Name;
        }
    }
}
