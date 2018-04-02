/*  
    Hongyou Xiong
    COMP 565 Project 1 Additional files
    Hut class to generate multiple huts
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
    public class Huts : MovableModel3D
    {
        // Constructor
        public Huts(Stage stage, string label, string meshFile, int nHuts)
           : base(stage, label, meshFile)
        {
            int separationX = 0;
            int separationZ = 0;

            random = new Random();
            // add nClouds random cloud instances
            for (int i = 0; i < nHuts; i++)
            {
                int x = (128 + random.Next(256) + separationX) * stage.Spacing;  // 128 .. 384
                int z = (128 + random.Next(256) + separationZ) * stage.Spacing;

                int coinFlip = random.Next(2);
                if (coinFlip == 0)
                {
                    separationX += 5; // Increase X range by 5
                }
                else
                {
                    separationZ += 5; // Else increase Z range by 5
                }

                addObject(
                    new Vector3(x, stage.surfaceHeight(x, z), z),
                    new Vector3(0, 1, 0),
                    random.Next(5) * 0.01f);
            }
        }
    }
}
