using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;

namespace Agrock {

    public class Agrock {

        private const int NUM_PATTERNS = 36;
        private const int PATTERN_LENGTH = 4;
        private const int RECTANGLES = (PATTERN_LENGTH * PATTERN_LENGTH) / 2;

        private const int RECURSION_LIMIT = 1; // Min value = 1; max value = text_len / pat_len

        private static Size GRID_SIZE = new Size(19, 6);
        private static int BLOCK_SIZE = 256;

        // Note: MAX_COLOR_DIFF should be around 0.25 and REDUCTION_RATE should be around 4 / COLORS
        private static int COLORS = 16; // Different base colors in a pattern
        private static float MAX_COLOR_DIFF = 0.2f; // Maximum variation between base colors of a pattern
        private static float REDUCTION_RATE = COLORS * 0.1f; // Rate at which color diff reduces in each iteration

        private static bool COLORED = true;

        private static void ParseArgs(string[] args) {

            Dictionary<string, string> dict = new Dictionary<string, string>();
            for (int i = 0; i < args.Length / 2; i += 2) dict.Add(args[i], args[i + 1]);

            if (dict.ContainsKey("-w") && dict.ContainsKey("-h"))
                GRID_SIZE = new Size(int.Parse(dict["-w"]), int.Parse(dict["-h"]));

            if (dict.ContainsKey("-b")) BLOCK_SIZE = int.Parse(dict["-b"]);

            if (dict.ContainsKey("-c")) COLORS = int.Parse(dict["-c"]);
            if (dict.ContainsKey("-cd")) MAX_COLOR_DIFF = float.Parse(dict["-cd"]);
            if (dict.ContainsKey("-r")) REDUCTION_RATE = COLORS * float.Parse(dict["-r"]);

            if (dict.ContainsKey("-cl")) COLORED = bool.Parse(dict["-cl"]);
        }

        public static void Main(string[] args) {

            args = new[] {
                "-w", "16",
                "-h", "9",
                "-b", "256",
                "-c", "16",
                "-cd", "0.2",
                "-r", "0.2",
                "-cl", "true"};

            ParseArgs(args);

            string filename = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) +
                "/" + DateTime.Now.Year + "-" + DateTime.Now.Month + "-" + DateTime.Now.Day +
                "-" + DateTime.Now.Hour + "-" + DateTime.Now.Minute + "-" + DateTime.Now.Second +
                "-" + DateTime.Now.Millisecond + ".png";

            Random random = new Random();

            Bitmap image = new Bitmap(GRID_SIZE.Width * BLOCK_SIZE, GRID_SIZE.Height * BLOCK_SIZE);

            Console.WriteLine("Algorithmically generating rock image");
            Console.WriteLine("Size: " + image.Width + "x" + image.Height + "px");
            Console.WriteLine("File: " + filename);
            Console.WriteLine("Generating: 0%");

            float totalCells = GRID_SIZE.Width * GRID_SIZE.Height;
            int completedCells = 0;
            int lastPercentage = 0;

            float colorOffset = (1 - MAX_COLOR_DIFF) / 2;

            for (int i = 0; i < GRID_SIZE.Width; i++) {

                for (int j = 0; j < GRID_SIZE.Height; j++) {

                    float[,] cell = GenerateBlock(random, random.Next(NUM_PATTERNS), BLOCK_SIZE);
                    float[,] cellColor = GenerateBlock(random, random.Next(NUM_PATTERNS), BLOCK_SIZE);

                    for (int ci = 0; ci < BLOCK_SIZE; ci++) {

                        for (int cj = 0; cj < BLOCK_SIZE; cj++) {

                            if (!COLORED) {

                                int grey = (int) Math.Round(Clamp(cell[ci, cj], 0, 1) * 255);

                                image.SetPixel(i * BLOCK_SIZE + ci, j * BLOCK_SIZE + cj,
                                    Color.FromArgb(grey, grey, grey));
                            }
                            else {

                                float b = (float) Clamp(cell[ci, cj], 0, 1);
                                float h = (float) (360 * Clamp((cellColor[ci, cj] - colorOffset) / MAX_COLOR_DIFF, 0, 1));

                                image.SetPixel(i * BLOCK_SIZE + ci, j * BLOCK_SIZE + cj,
                                    ColorFromHSB(h, 0.2f, b));
                            }
                        }
                    }

                    completedCells++;
                    int percentage = (int) (completedCells / totalCells * 100);

                    if (percentage != lastPercentage) {

                        Console.SetCursorPosition(0, Console.CursorTop - 1);
                        Console.WriteLine("Generating: " + percentage + "%");
                        lastPercentage = percentage;
                    }
                }
            }

            Console.WriteLine("Saving...");
            image.Save(filename, ImageFormat.Png);
        }

        public static float[,] GenerateBlock(Random rng, int patternIndex, int blockSize) {

            float[,] block = new float[blockSize, blockSize];
            FillSquare(rng, block, 0, 0, blockSize, 0.5f, MAX_COLOR_DIFF, patternIndex);
            return block;
        }

        private static void FillSquare(Random rng, float[,] texture,
            int initX, int initY, int length, float baseColor, float colorDiff, int patternIndex) {

            int[,] pattern = Patterns[patternIndex];
            int[] coloring = GenerateColoring(rng); // Gets a random coloring with no restrictions
            float colorVar = colorDiff / COLORS; // Color variation between two consecutive colors

            int cellLength = length / PATTERN_LENGTH;

            for (int i = 0; i < PATTERN_LENGTH; i++) {

                for (int j = 0; j < PATTERN_LENGTH; j++) {

                    // Gets the base color for this cell
                    float cellBaseColor = baseColor + colorDiff / 2 - colorVar * (0.5f + coloring[pattern[i, j]]);

                    if (cellLength > RECURSION_LIMIT) {

                        // Fills this cell with a new pattern
                        FillSquare(rng, texture,
                            initX + i * cellLength, initY + j * cellLength, cellLength,
                            cellBaseColor, colorDiff / REDUCTION_RATE,
                            rng.Next(NUM_PATTERNS));
                    }
                    else {

                        // Paints this cell pixels with the base color
                        float color = cellBaseColor;

                        for (int m = 0; m < cellLength; m++)
                            for (int n = 0; n < cellLength; n++)
                                texture[initX + i * cellLength + m, initY + j * cellLength + n] = color;
                    }
                }
            }
        }

        private static int[] GenerateColoring(Random rng) {

            int[] coloring = new int[RECTANGLES];

            for (int i = 0; i < coloring.Length; i++)
                coloring[i] = rng.Next(COLORS);

            return coloring;
        }

        public static double Clamp(double value, double min, double max) {

            return Math.Min(max, Math.Max(min, value));
        }

        /// <summary>
        /// Converts the representation of a color from HSB to RGB.
        /// Original code by Chris Jackson:
        /// https://blogs.msdn.microsoft.com/cjacks/2006/04/12/converting-from-hsb-to-rgb-in-net/
        /// </summary>
        private static Color ColorFromHSB(float h, float s, float b) {

            if (0 == s) return Color.FromArgb(
                Convert.ToInt32(b * 255), Convert.ToInt32(b * 255), Convert.ToInt32(b * 255));

            float fMax, fMid, fMin;

            if (0.5 < b) {

                fMax = b - (b * s) + s;
                fMin = b + (b * s) - s;
            }
            else {

                fMax = b + (b * s);
                fMin = b - (b * s);
            }

            int iSextant = (int) Math.Floor(h / 60f);

            if (300f <= h) h -= 360f;

            h /= 60f;
            h -= 2f * (float) Math.Floor(((iSextant + 1f) % 6f) / 2f);

            if (0 == iSextant % 2) fMid = h * (fMax - fMin) + fMin;
            else fMid = fMin - h * (fMax - fMin);

            int iMax = Convert.ToInt32(fMax * 255);
            int iMid = Convert.ToInt32(fMid * 255);
            int iMin = Convert.ToInt32(fMin * 255);

            switch (iSextant) {

                case 1:
                    return Color.FromArgb(iMid, iMax, iMin);
                case 2:
                    return Color.FromArgb(iMin, iMax, iMid);
                case 3:
                    return Color.FromArgb(iMin, iMid, iMax);
                case 4:
                    return Color.FromArgb(iMid, iMin, iMax);
                case 5:
                    return Color.FromArgb(iMax, iMin, iMid);
                default:
                    return Color.FromArgb(iMax, iMid, iMin);
            }
        }

        /*
         * The 36 possible patterns (without color randomization).
         * Although it's possible to generate these patterns algorithmically,
         * it's hard to find them with equal probability.
         */
        private static readonly int[][,] Patterns = {

            new[,] {

                {0,0,1,1},
                {2,2,3,3},
                {4,4,5,5},
                {6,6,7,7}
            },

            new[,] {

                {0,0,1,1},
                {2,2,3,3},
                {4,4,5,6},
                {7,7,5,6}
            },

            new[,] {

                {0,0,1,1},
                {2,2,3,3},
                {4,5,5,6},
                {4,7,7,6}
            },

            new[,] {

                {0,0,1,1},
                {2,2,3,3},
                {4,5,6,6},
                {4,5,7,7}
            },

            new[,] {

                {0,0,1,1},
                {2,2,3,3},
                {4,5,6,7},
                {4,5,6,7}
            },

            new[,] {

                {0,0,1,1},
                {2,2,3,4},
                {5,5,3,4},
                {6,6,7,7}
            },

            new[,] {

                {0,0,1,1},
                {2,2,3,4},
                {5,6,3,4},
                {5,6,7,7}
            },

            new[,] {

                {0,0,1,1},
                {2,3,3,4},
                {2,5,5,4},
                {6,6,7,7}
            },

            new[,] {

                {0,0,1,1},
                {2,3,4,4},
                {2,3,5,5},
                {6,6,7,7}
            },

            new[,] {

                {0,0,1,1},
                {2,3,4,4},
                {2,3,5,6},
                {7,7,5,6}
            },

            new[,] {

                {0,0,1,1},
                {2,3,4,5},
                {2,3,4,5},
                {6,6,7,7}
            },

            new[,] {

                {0,0,1,2},
                {3,3,1,2},
                {4,4,5,5},
                {6,6,7,7}
            },

            new[,] {

                {0,0,1,2},
                {3,3,1,2},
                {4,4,5,6},
                {7,7,5,6}
            },

            new[,] {

                {0,0,1,2},
                {3,3,1,2},
                {4,5,5,6},
                {4,7,7,6}
            },

            new[,] {

                {0,0,1,2},
                {3,3,1,2},
                {4,5,6,6},
                {4,5,7,7}
            },

            new[,] {

                {0,0,1,2},
                {3,3,1,2},
                {4,5,6,7},
                {4,5,6,7}
            },

            new[,] {

                {0,0,1,2},
                {3,4,1,2},
                {3,4,5,5},
                {6,6,7,7}
            },

            new[,] {

                {0,0,1,2},
                {3,4,1,2},
                {3,4,5,6},
                {7,7,5,6}
            },

            new[,] {

                {0,1,1,2},
                {0,3,3,2},
                {4,4,5,5},
                {6,6,7,7}
            },

            new[,] {

                {0,1,1,2},
                {0,3,3,2},
                {4,4,5,6},
                {7,7,5,6}
            },

            new[,] {

                {0,1,1,2},
                {0,3,3,2},
                {4,5,5,6},
                {4,7,7,6}
            },

            new[,] {

                {0,1,1,2},
                {0,3,3,2},
                {4,5,6,6},
                {4,5,7,7}
            },

            new[,] {

                {0,1,1,2},
                {0,3,3,2},
                {4,5,6,7},
                {4,5,6,7}
            },

            new[,] {

                {0,1,1,2},
                {0,3,4,2},
                {5,3,4,6},
                {5,7,7,6}
            },

            new[,] {

                {0,1,2,2},
                {0,1,3,3},
                {4,4,5,5},
                {6,6,7,7}
            },

            new[,] {

                {0,1,2,2},
                {0,1,3,3},
                {4,4,5,6},
                {7,7,5,6}
            },

            new[,] {

                {0,1,2,2},
                {0,1,3,3},
                {4,5,5,6},
                {4,7,7,6}
            },

            new[,] {

                {0,1,2,2},
                {0,1,3,3},
                {4,5,6,6},
                {4,5,7,7}
            },

            new[,] {

                {0,1,2,2},
                {0,1,3,3},
                {4,5,6,7},
                {4,5,6,7}
            },

            new[,] {

                {0,1,2,2},
                {0,1,3,4},
                {5,5,3,4},
                {6,6,7,7}
            },

            new[,] {

                {0,1,2,2},
                {0,1,3,4},
                {5,6,3,4},
                {5,6,7,7}
            },

            new[,] {

                {0,1,2,3},
                {0,1,2,3},
                {4,4,5,5},
                {6,6,7,7}
            },

            new[,] {

                {0,1,2,3},
                {0,1,2,3},
                {4,4,5,6},
                {7,7,5,6}
            },

            new[,] {

                {0,1,2,3},
                {0,1,2,3},
                {4,5,5,6},
                {4,7,7,6}
            },

            new[,] {

                {0,1,2,3},
                {0,1,2,3},
                {4,5,6,6},
                {4,5,7,7}
            },

            new[,] {

                {0,1,2,3},
                {0,1,2,3},
                {4,5,6,7},
                {4,5,6,7}
            }
        };
    }
}
