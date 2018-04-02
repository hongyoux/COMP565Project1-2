/*  
    Hongyou Xiong
    COMP 565 Project 1 Additional files
    Bushes class to generate multiple bushes
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
    public class Bushes : MovableModel3D
    {
        // Constructor
        public Bushes(Stage stage, string label, string meshFile, int nHuts)
           : base(stage, label, meshFile)
        {
            random = new Random();
            // add nClouds random cloud instances
            for (int i = 0; i < nHuts; i++)
            {
                int x = (32 + random.Next(449)) * stage.Spacing;  // 32 .. 480
                int z = (32 + random.Next(449)) * stage.Spacing;

                while (stage.surfaceHeight(x, z) > (175 * stage.Spacing)) // Mountainous region
                {
                    // keep rolling x and z until not mountainous
                    x = (32 + random.Next(449)) * stage.Spacing;
                    z = (32 + random.Next(449)) * stage.Spacing;
                }

                addObject(
                    new Vector3(x, stage.surfaceHeight(x, z), z),
                    new Vector3(0, 1, 0),
                    random.Next(5) * 0.01f);
            }
        }
    }
}
