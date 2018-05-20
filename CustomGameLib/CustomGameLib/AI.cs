﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Threading;
using System.Windows.Forms;

namespace Deltin.CustomGameAutomation
{
    partial class CustomGame
    {
        public CG_AI AI;
        public class CG_AI
        {
            private CustomGame cg;
            internal CG_AI(CustomGame cg)
            {
                this.cg = cg;
            }

            /// <summary>
            /// Add AI to the game.
            /// </summary>
            /// <param name="hero">Hero type to add.</param>
            /// <param name="difficulty">Difficulty of hero.</param>
            /// <param name="team">Team that AI joins.</param>
            /// <param name="count">Amount of AI that is added. Set to -1 for max. Default is -1</param>
            /// <returns></returns>
            // Adds a bot
            public bool AddAI(AIHero hero, Difficulty difficulty, BotTeam team, int count = -1)
            {
                cg.updateScreen();

                // Find the maximum amount of bots that can be placed on a team, and store it in the maxBots variable

                if (cg.addbutton())
                /*
                 * If the blue shade of the "Move" button is there, that means that the Add AI button is there. 
                 * If the Add AI button is missing, we can't add AI, so return false. If it is there, add the bots.
                 * The AI button will be missing if the server is full
                 */
                {
                    // Open AddAI menu.
                    cg.Cursor = new Point(835, 182);
                    cg.WaitForUpdate(835, 182, 20, 2000);
                    cg.LeftClick(835, 182, 500);

                    List<Keys> keypresses = new List<Keys>(); // The keypresses that need to be pressed to add the AI.

                    if (hero != AIHero.Recommended)
                    {
                        int heroid = (int)hero;

                        // Open Hero Select menu
                        keypresses.Add(Keys.Space);
                        // Select the hero
                        for (int i = 0; i < heroid; i++)
                            keypresses.Add(Keys.Down);
                        keypresses.Add(Keys.Space);
                    }

                    keypresses.Add(Keys.Down); // Select the difficulty option
                    if (difficulty != Difficulty.Easy)
                    {
                        int difficultyid = (int)difficulty;

                        keypresses.Add(Keys.Space); // Open difficulty menu
                                                    // Select the difficulty
                        for (int i = 0; i < difficultyid; i++)
                            keypresses.Add(Keys.Down);
                        keypresses.Add(Keys.Space);
                    }

                    keypresses.Add(Keys.Down); keypresses.Add(Keys.Down); // Select the team option
                    if (team != BotTeam.Both)
                    {
                        int teamid = (int)team + 1;

                        keypresses.Add(Keys.Space); // Open team menu
                                                    // Select team
                        for (int i = 0; i < teamid; i++)
                            keypresses.Add(Keys.Down);
                        keypresses.Add(Keys.Space);
                    }

                    if (count > 0)
                    {
                        keypresses.Add(Keys.Up); // Select the count option

                        // Set add AI count to 0
                        for (int i = 0; i < 12; i++)
                            keypresses.Add(Keys.Left);
                        // Set add AI count to count.
                        for (int i = 0; i < count; i++)
                            keypresses.Add(Keys.Right);

                        keypresses.Add(Keys.Down); // Select the team option
                    }

                    keypresses.Add(Keys.Down); // Select the add/back option

                    keypresses.Add(Keys.Space); // Add the AI.

                    cg.KeyPress(keypresses.ToArray());

                    cg.ResetMouse();

                    return true;
                }
                else
                    return false;
            }

            public void GetAIDifficultyMarkup(int scalar, string saveAt)
            {
                cg.updateScreen();
                int[] scales = new int[] { 33, 49, 34 };
                Bitmap tmp = cg.bmp.Clone(new Rectangle(402, 244, scales[scalar], 16), cg.bmp.PixelFormat);
                for (int x = 0; x < tmp.Width; x++)
                    for (int y = 0; y < tmp.Height; y++)
                    {
                        if (tmp.CompareColor(x, y, AIColor, 25))
                            tmp.SetPixel(x, y, Color.Black);
                        else
                            tmp.SetPixel(x, y, Color.White);
                    }
                if (cg.debugmode)
                {
                    int scale = 5;
                    cg.g.DrawImage(tmp, new Rectangle(750, 0, tmp.Width * scale, tmp.Height * scale));
                }
                tmp.Save(saveAt);
                tmp.Dispose();
            }

            /// <summary>
            /// Gets the difficulty of the AI in the input slot.
            /// <para>If the input slot is not an AI, returns null.
            /// If checking an AI's difficulty in the queue, it will always return easy, or null if it is a player.</para>
            /// </summary>
            /// <param name="slot">Slot to check</param>
            /// <param name="noUpdate"></param>
            /// <returns>Returns a value in the Difficulty enum if the difficulty is found. Returns null if the input slot is not an AI.</returns>
            /// <exception cref="InvalidSlotException">Thrown when slot is out of range.</exception>
            public Difficulty? GetAIDifficulty(int slot, bool noUpdate = false)
            {
                if (slot < 0 || (slot > 11 && slot < Queueid) || slot > Queueid + 6)
                    throw new InvalidSlotException(string.Format("Slot {0} is out of range of possible slots to check for AI.", slot));

                if (!noUpdate)
                    cg.updateScreen();

                if (slot <= 11)
                {

                    bool draw = cg.debugmode; // For debug mode

                    List<int> rl = new List<int>(); // Likelyhood in percent for difficulties.
                    List<Difficulty> dl = new List<Difficulty>(); // Difficulty

                    bool foundWhite = false;
                    int foundWhiteIndex = 0;
                    int maxWhite = 3;
                    // For each check length in IsAILocations
                    for (int xi = 0; xi < IsAILocations[slot, 2] && foundWhiteIndex < maxWhite; xi++)
                    {
                        if (foundWhite)
                            foundWhiteIndex++;

                        Color cc = cg.bmp.GetPixelAt(IsAILocations[slot, 0] + xi, IsAILocations[slot, 1]);
                        // Check for white color of text
                        if (cg.bmp.CompareColor(IsAILocations[slot, 0] + xi, IsAILocations[slot, 1], AIColor, 110)
                            && (slot > 5 || cc.B - cc.R < 30))
                        {
                            foundWhite = true;

                            // For each difficulty markup
                            for (int b = 0; b < DifficultyMarkups.Length; b++)
                            {
                                Bitmap tmp = null;
                                if (draw)
                                    tmp = cg.bmp.Clone(new Rectangle(0, 0, cg.bmp.Width, cg.bmp.Height), cg.bmp.PixelFormat);

                                // Check if bitmap matches checking area
                                double success = 0;
                                double total = 0;
                                for (int x = 0; x < DifficultyMarkups[b].Width; x++)
                                    for (int y = DifficultyMarkups[b].Height - 1; y >= 0; y--)
                                    {
                                        // If the color pixel of the markup is not white, check if valid.
                                        Color pc = DifficultyMarkups[b].GetPixel(x, y);
                                        if (pc != Color.FromArgb(255, 255, 255, 255))
                                        {
                                            // tc is true if the pixel is black, false if it is red.
                                            bool tc = pc == Color.FromArgb(255, 0, 0, 0);

                                            total++; // Indent the total
                                                     // If the checking color in the bmp bitmap is equal to the pc color, add to success.
                                            if (cg.bmp.CompareColor(IsAILocations[slot, 0] + xi + x, IsAILocations[slot, 1] - Extensions.InvertNumber(y, DifficultyMarkups[b].Height - 1), AIColor, 50) == tc)
                                            {
                                                success++;

                                                if (draw)
                                                {
                                                    if (tc)
                                                        tmp.SetPixel(IsAILocations[slot, 0] + xi + x, IsAILocations[slot, 1] - Extensions.InvertNumber(y, DifficultyMarkups[b].Height - 1), Color.Blue);
                                                    else
                                                        tmp.SetPixel(IsAILocations[slot, 0] + xi + x, IsAILocations[slot, 1] - Extensions.InvertNumber(y, DifficultyMarkups[b].Height - 1), Color.Lime);
                                                }
                                            }
                                            else if (draw)
                                            {
                                                if (tc)
                                                    tmp.SetPixel(IsAILocations[slot, 0] + xi + x, IsAILocations[slot, 1] - Extensions.InvertNumber(y, DifficultyMarkups[b].Height - 1), Color.Red);
                                                else
                                                    tmp.SetPixel(IsAILocations[slot, 0] + xi + x, IsAILocations[slot, 1] - Extensions.InvertNumber(y, DifficultyMarkups[b].Height - 1), Color.Orange);
                                            }

                                            if (draw)
                                            {
                                                tmp.SetPixel(IsAILocations[slot, 0] + xi + x, IsAILocations[slot, 1] - DifficultyMarkups[b].Height * 2 + y, DifficultyMarkups[b].GetPixel(x, y));
                                                cg.g.DrawImage(tmp, new Rectangle(0, -750, tmp.Width * 3, tmp.Height * 3));
                                                Thread.Sleep(1);
                                            }
                                        }
                                    }
                                // Get the result
                                double result = (success / total) * 100;

                                rl.Add((int)result);
                                dl.Add((Difficulty)b);

                                if (draw)
                                {
                                    tmp.SetPixel(IsAILocations[slot, 0] + xi, IsAILocations[slot, 1], Color.MediumPurple);
                                    cg.g.DrawImage(tmp, new Rectangle(0, -750, tmp.Width * 3, tmp.Height * 3));
                                    Console.WriteLine((Difficulty)b + " " + result);
                                    Thread.Sleep(1000);
                                }
                            }
                        }
                    }

                    // Return the difficulty that is most possible.
                    if (rl.Count > 0)
                    {
                        int max = rl.Max();
                        if (max >= 75)
                            return dl[rl.IndexOf(max)];
                        else
                            return null;
                    }
                    else
                        return null;
                }

                else if (cg.QueueCount > 0)
                {
                    int y = IsAILocationsQueue[slot - Queueid];
                    for (int x = IsAILocationQueueX; x < 150 + IsAILocationQueueX; x++)
                        if (cg.bmp.CompareColor(x, y, new int[] { 180, 186, 191 }, 10))
                            return null;
                    return Difficulty.Easy;
                }

                else
                    return null;
            }

            internal Bitmap[] DifficultyMarkups = new Bitmap[]
            {
                new Bitmap(Properties.Resources.easy_difficulty),
                new Bitmap(Properties.Resources.medium_difficulty),
                new Bitmap(Properties.Resources.hard_difficulty)
            };
            static int[] AIColor = new int[] { 191, 191, 191 };

            static int[,] IsAILocations = new int[,]
            {
                // X    Y  Length
                // Blue
                { 145, 259, 100 },
                { 145, 288, 100 },
                { 145, 316, 100 },
                { 145, 345, 100 },
                { 145, 373, 100 },
                { 145, 402, 100 },
                // Red
                { 401, 259, 25 },
                { 401, 288, 25 },
                { 401, 316, 25 },
                { 401, 345, 25 },
                { 401, 373, 25 },
                { 401, 402, 25 }
            };

            static int IsAILocationQueueX = 686;
            static int[] IsAILocationsQueue = new int[]
            {
                244,
                257,
                270,
                283,
                297,
                310
            };

            /// <summary>
            /// Removes all AI from the game.
            /// </summary>
            /// <returns>Returns true if successful.</returns>
            public bool RemoveAllBotsAuto()
            {
                cg.updateScreen();

                for (int i = 0; i < IsAILocations.GetLength(0); i++)
                    if (GetAIDifficulty(i, true) != null)
                        if (cg.Interact.RemoveAllBots(i))
                            return true;

                for (int i = 0; i < IsAILocationsQueue.Length; i++)
                    if (GetAIDifficulty(Queueid + i, true) != null)
                        if (cg.Interact.RemoveAllBots(Queueid + i))
                            return true;
                return false;
            }

            /// <summary>
            /// Checks if input slot is AI.
            /// </summary>
            /// <param name="slot">Slot to check.</param>
            /// <returns>Returns true if slot is AI.</returns>
            public bool IsAI(int slot)
            {
                // Returns true if GetAIDifficulty can find the difficulty of a slot. Returns false if difficulty is null.
                return GetAIDifficulty(slot) != null;
            }

            // Edit AI
            /// <summary>
            /// Edits the hero an AI is playing and the difficulty of the AI.
            /// </summary>
            /// <param name="slot">Slot to edit.</param>
            /// <param name="setToHero">Hero to change to.</param>
            /// <param name="setToDifficulty">Difficulty to change to.</param>
            /// <returns>Returns true on success.</returns>
            public bool EditAI(int slot, AIHero setToHero, Difficulty setToDifficulty)
            {
                return EditAI(slot, setToHero, setToDifficulty, true);
            }
            /// <summary>
            /// Edits the hero an AI is playing.
            /// </summary>
            /// <param name="slot">Slot to edit.</param>
            /// <param name="setToHero">Hero to change to.</param>
            /// <returns>Returns true on success.</returns>
            public bool EditAI(int slot, AIHero setToHero)
            {
                return EditAI(slot, setToHero, null, true);
            }
            /// <summary>
            /// Edits the difficulty of an AI.
            /// </summary>
            /// <param name="slot">Slot to edit.</param>
            /// <param name="setToDifficulty">Difficulty to change to.</param>
            /// <returns>Returns true on success.</returns>
            public bool EditAI(int slot, Difficulty setToDifficulty)
            {
                return EditAI(slot, null, setToDifficulty, true);
            }

            bool EditAI(int slot, object setToHero, object setToDifficulty, bool x)
            {
                // Make sure there is a player or AI in selected slot, or if they are a valid slot to select in queue.
                if (cg.PlayerSlots.Contains(slot) || (slot >= Queueid && slot - (Queueid) < cg.QueueCount))
                {
                    // Click the slot of the selected slot.
                    var slotlocation = cg.Interact.FindSlotLocation(slot);
                    cg.LeftClick(slotlocation.X, slotlocation.Y);
                    // Check if Edit AI window has opened by checking if the confirm button exists.
                    cg.updateScreen();
                    if (cg.bmp.CompareColor(447, 354, CALData.ConfirmColor, 20))
                    {
                        var sim = new List<Keys>();
                        // Set hero if setToHero does not equal null.
                        if (setToHero != null)
                        {
                            AIHero selectHero = (AIHero)setToHero;
                            // Open hero menu
                            sim.Add(Keys.Space);
                            // <image url="$(ProjectDir)\ImageComments\AI.cs\EditAIHero.png" scale="0.5" />
                            // Select the topmost hero option
                            for (int i = 0; i < Enum.GetNames(typeof(AIHero)).Length; i++)
                                sim.Add(Keys.Up);
                            // Select the hero in selectHero.
                            for (int i = 0; i < (int)selectHero; i++)
                                sim.Add(Keys.Down);
                            sim.Add(Keys.Space);
                        }
                        sim.Add(Keys.Down); // Select difficulty option
                                                      // Set difficulty if setToDifficulty does not equal null.
                        if (setToDifficulty != null)
                        {
                            Difficulty selectDifficulty = (Difficulty)setToDifficulty;
                            // Open difficulty menu
                            sim.Add(Keys.Space);
                            // <image url="$(ProjectDir)\ImageComments\AI.cs\EditAIDifficulty.png" scale="0.6" />
                            // Select the topmost difficulty
                            for (int i = 0; i < Enum.GetNames(typeof(Difficulty)).Length; i++)
                                sim.Add(Keys.Up);
                            // Select the difficulty in selectDifficulty.
                            for (int i = 0; i < (int)selectDifficulty; i++)
                                sim.Add(Keys.Down);
                            sim.Add(Keys.Space);
                        }
                        // Confirm the changes
                        sim.Add(Keys.Return);

                        // Send the keypresses.
                        cg.KeyPress(sim.ToArray());

                        cg.ResetMouse();
                        return true;
                    }
                    else
                    {
                        cg.ResetMouse();
                        return false;
                    }
                }
                else
                {
                    return false;
                }
            }
        }
    }

    public enum AIHero
    {
        Recommended,
        Ana,
        Bastion,
        Lucio,
        McCree,
        Mei,
        Reaper,
        Roadhog,
        Soldier76,
        Sombra,
        Torbjorn,
        Zarya,
        Zenyatta
    }
    public enum Difficulty
    {
        Easy,
        Medium,
        Hard
    }
}
