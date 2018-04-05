/*  
    Hongyou Xiong
    COMP 565 Project 2 Additional files
    ObstacleHuts class to block pathways
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
    public class ObstacleHuts : MovableModel3D
    {
        private int[,] southernObstacles = { { 390, 455 }, { 390, 460 }, { 390, 465 }, { 390, 470 }, { 390, 475 }, { 390, 480 } };
        private int[,] westernObstacles =  { {  90, 460 }, {  95, 455 }, { 100, 450 }, { 105, 450 }, { 110, 455 }, { 115, 460 } };
        private int[,] northernObstacles = { { 390, 485 }, { 390, 490 }, { 390, 495 }, { 390, 500 }, { 390, 505 }, { 390, 510 } };
        private int[,] easternObstacles =  { { 160,  85 }, { 165,  90 }, { 170,  95 }, { 175, 100 },
                                             { 180, 105 }, { 185, 110 }, { 190, 115 }, { 195, 120 },
                                             { 200, 125 }, { 205, 130 }, { 210, 135 }, { 215, 140 },
                                             { 220, 145 }, { 225, 150 }, { 230, 155 }, { 235, 160 }};

        // Constructor
        public ObstacleHuts(Stage stage, string label, string meshFile)
           : base(stage, label, meshFile)
        {
            IsCollidable = true;

            random = new Random();

            for (int i = 0; i < southernObstacles.Length / 2; i++)
            {
                int x_south = southernObstacles[i, 0] * stage.Spacing;
                int z_south = southernObstacles[i, 1] * stage.Spacing;

                addObject(
                    new Vector3(x_south, stage.surfaceHeight(x_south, z_south), z_south),
                    new Vector3(0, 1, 0),
                    random.Next(5) * 0.01f);
            }
            for (int i = 0; i < northernObstacles.Length / 2; i++)
            {
                int x_north = northernObstacles[i, 0] * stage.Spacing;
                int z_north = northernObstacles[i, 1] * stage.Spacing;

                addObject(
                    new Vector3(x_north, stage.surfaceHeight(x_north, z_north), z_north),
                    new Vector3(0, 1, 0),
                    random.Next(5) * 0.01f);
            }
            for (int i = 0; i < westernObstacles.Length / 2; i++)
            {
                int x_west = westernObstacles[i, 0] * stage.Spacing;
                int z_west = westernObstacles[i, 1] * stage.Spacing;

                addObject(
                    new Vector3(x_west, stage.surfaceHeight(x_west, z_west), z_west),
                    new Vector3(0, 1, 0),
                    random.Next(5) * 0.01f);
            }
            for (int i = 0; i < easternObstacles.Length / 2; i++)
            {
                int x_east = easternObstacles[i, 0] * stage.Spacing;
                int z_east = easternObstacles[i, 1] * stage.Spacing;

                addObject(
                    new Vector3(x_east, stage.surfaceHeight(x_east, z_east), z_east),
                    new Vector3(0, 1, 0),
                    random.Next(5) * 0.01f);
            }

        }
    }
}
