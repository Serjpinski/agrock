using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;

namespace Agrock {

    class Agrock {

        private static readonly int NUM_PATTERNS = 36;
        private static readonly int PATTERN_LENGTH = 4;
        private static readonly int RECTANGLES = (PATTERN_LENGTH * PATTERN_LENGTH) / 2;

        private static readonly int RECURSION_LIMIT = 1; // Min value = 1; max value = text_len / pat_len

        // Note: MAX_COLOR_DIFF should be around 0.25 and REDUCTION_RATE should be around 4 / COLORS
        private static int COLORS = 16; // Different base colors in a pattern
        private static float MAX_COLOR_DIFF = 0.2f; // Maximum variation between base colors of a pattern
        private static float REDUCTION_RATE = COLORS * 0.1f; // Rate at which color diff reduces in each iteration

        private static bool COLORED = true;

        static void Main(string[] args) {

            args = new string[] {
                "16", "9", "256",
                "16", "0.2", "0.2",
                "true"};

            Size gridSize = new Size(int.Parse(args[0]), int.Parse(args[1]));
            int blockSize = int.Parse(args[2]);

            COLORS = int.Parse(args[3]);
            MAX_COLOR_DIFF = float.Parse(args[4]);
            REDUCTION_RATE = COLORS * float.Parse(args[5]);

            COLORED = bool.Parse(args[6]);

            string filename = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) +
                "/" + DateTime.Now.Year + "-" + DateTime.Now.Month + "-" + DateTime.Now.Day +
                "-" + DateTime.Now.Hour + "-" + DateTime.Now.Minute + "-" + DateTime.Now.Second +
                "-" + DateTime.Now.Millisecond + ".png";

            Random random = new Random();

            Bitmap image = new Bitmap(gridSize.Width * blockSize, gridSize.Height * blockSize);

            Console.WriteLine("Algorithmically generating rock image");
            Console.WriteLine("Size: " + image.Width + "x" + image.Height + "px");
            Console.WriteLine("File: " + filename);
            Console.WriteLine("Generating: 0%");

            float totalCells = gridSize.Width * gridSize.Height;
            int completedCells = 0;
            int lastPercentage = 0;

            float colorOffset = (1 - MAX_COLOR_DIFF) / 2;

            for (int i = 0; i < gridSize.Width; i++) {

                for (int j = 0; j < gridSize.Height; j++) {

                    float[,] cell = GenerateBlock(random, random.Next(NUM_PATTERNS), blockSize);
                    float[,] cellColor = GenerateBlock(random, random.Next(NUM_PATTERNS), blockSize);

                    for (int ci = 0; ci < blockSize; ci++) {

                        for (int cj = 0; cj < blockSize; cj++) {

                            if (!COLORED) {

                                int grey = (int) Math.Round(Clamp(cell[ci, cj], 0, 1) * 255);

                                image.SetPixel(i * blockSize + ci, j * blockSize + cj,
                                    Color.FromArgb(grey, grey, grey));
                            }
                            else {

                                float b = (float) Clamp(cell[ci, cj], 0, 1);
                                float h = (float) (360 * Clamp((cellColor[ci, cj] - colorOffset) / MAX_COLOR_DIFF, 0, 1));

                                image.SetPixel(i * blockSize + ci, j * blockSize + cj,
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

            int[,] pattern = PATTERNS[patternIndex];
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
            int iSextant, iMax, iMid, iMin;

            if (0.5 < b) {

                fMax = b - (b * s) + s;
                fMin = b + (b * s) - s;
            }
            else {

                fMax = b + (b * s);
                fMin = b - (b * s);
            }

            iSextant = (int) Math.Floor(h / 60f);

            if (300f <= h) h -= 360f;

            h /= 60f;
            h -= 2f * (float) Math.Floor(((iSextant + 1f) % 6f) / 2f);

            if (0 == iSextant % 2) fMid = h * (fMax - fMin) + fMin;
            else fMid = fMin - h * (fMax - fMin);

            iMax = Convert.ToInt32(fMax * 255);
            iMid = Convert.ToInt32(fMid * 255);
            iMin = Convert.ToInt32(fMin * 255);

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
        private static int[][,] PATTERNS = new int[][,] {

            new int[,] {

                {0,0,1,1},
                {2,2,3,3},
                {4,4,5,5},
                {6,6,7,7}
            },

            new int[,] {

                {0,0,1,1},
                {2,2,3,3},
                {4,4,5,6},
                {7,7,5,6}
            },

            new int[,] {

                {0,0,1,1},
                {2,2,3,3},
                {4,5,5,6},
                {4,7,7,6}
            },

            new int[,] {

                {0,0,1,1},
                {2,2,3,3},
                {4,5,6,6},
                {4,5,7,7}
            },

            new int[,] {

                {0,0,1,1},
                {2,2,3,3},
                {4,5,6,7},
                {4,5,6,7}
            },

            new int[,] {

                {0,0,1,1},
                {2,2,3,4},
                {5,5,3,4},
                {6,6,7,7}
            },

            new int[,] {

                {0,0,1,1},
                {2,2,3,4},
                {5,6,3,4},
                {5,6,7,7}
            },

            new int[,] {

                {0,0,1,1},
                {2,3,3,4},
                {2,5,5,4},
                {6,6,7,7}
            },

            new int[,] {

                {0,0,1,1},
                {2,3,4,4},
                {2,3,5,5},
                {6,6,7,7}
            },

            new int[,] {

                {0,0,1,1},
                {2,3,4,4},
                {2,3,5,6},
                {7,7,5,6}
            },

            new int[,] {

                {0,0,1,1},
                {2,3,4,5},
                {2,3,4,5},
                {6,6,7,7}
            },

            new int[,] {

                {0,0,1,2},
                {3,3,1,2},
                {4,4,5,5},
                {6,6,7,7}
            },

            new int[,] {

                {0,0,1,2},
                {3,3,1,2},
                {4,4,5,6},
                {7,7,5,6}
            },

            new int[,] {

                {0,0,1,2},
                {3,3,1,2},
                {4,5,5,6},
                {4,7,7,6}
            },

            new int[,] {

                {0,0,1,2},
                {3,3,1,2},
                {4,5,6,6},
                {4,5,7,7}
            },

            new int[,] {

                {0,0,1,2},
                {3,3,1,2},
                {4,5,6,7},
                {4,5,6,7}
            },

            new int[,] {

                {0,0,1,2},
                {3,4,1,2},
                {3,4,5,5},
                {6,6,7,7}
            },

            new int[,] {

                {0,0,1,2},
                {3,4,1,2},
                {3,4,5,6},
                {7,7,5,6}
            },

            new int[,] {

                {0,1,1,2},
                {0,3,3,2},
                {4,4,5,5},
                {6,6,7,7}
            },

            new int[,] {

                {0,1,1,2},
                {0,3,3,2},
                {4,4,5,6},
                {7,7,5,6}
            },

            new int[,] {

                {0,1,1,2},
                {0,3,3,2},
                {4,5,5,6},
                {4,7,7,6}
            },

            new int[,] {

                {0,1,1,2},
                {0,3,3,2},
                {4,5,6,6},
                {4,5,7,7}
            },

            new int[,] {

                {0,1,1,2},
                {0,3,3,2},
                {4,5,6,7},
                {4,5,6,7}
            },

            new int[,] {

                {0,1,1,2},
                {0,3,4,2},
                {5,3,4,6},
                {5,7,7,6}
            },

            new int[,] {

                {0,1,2,2},
                {0,1,3,3},
                {4,4,5,5},
                {6,6,7,7}
            },

            new int[,] {

                {0,1,2,2},
                {0,1,3,3},
                {4,4,5,6},
                {7,7,5,6}
            },

            new int[,] {

                {0,1,2,2},
                {0,1,3,3},
                {4,5,5,6},
                {4,7,7,6}
            },

            new int[,] {

                {0,1,2,2},
                {0,1,3,3},
                {4,5,6,6},
                {4,5,7,7}
            },

            new int[,] {

                {0,1,2,2},
                {0,1,3,3},
                {4,5,6,7},
                {4,5,6,7}
            },

            new int[,] {

                {0,1,2,2},
                {0,1,3,4},
                {5,5,3,4},
                {6,6,7,7}
            },

            new int[,] {

                {0,1,2,2},
                {0,1,3,4},
                {5,6,3,4},
                {5,6,7,7}
            },

            new int[,] {

                {0,1,2,3},
                {0,1,2,3},
                {4,4,5,5},
                {6,6,7,7}
            },

            new int[,] {

                {0,1,2,3},
                {0,1,2,3},
                {4,4,5,6},
                {7,7,5,6}
            },

            new int[,] {

                {0,1,2,3},
                {0,1,2,3},
                {4,5,5,6},
                {4,7,7,6}
            },

            new int[,] {

                {0,1,2,3},
                {0,1,2,3},
                {4,5,6,6},
                {4,5,7,7}
            },

            new int[,] {

                {0,1,2,3},
                {0,1,2,3},
                {4,5,6,7},
                {4,5,6,7}
            }
        };
    }
}
