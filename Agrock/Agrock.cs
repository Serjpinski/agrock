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

        private static Size IMAGE_SIZE = new Size(4096, 2304);
        private static int PIXEL_SIZE = 1;
        private static int RECURSION = 4;

        private static int COLOR_NUMBER = 16; // Different base colors in a pattern
        private static float COLOR_DIFF = 0.2f; // Maximum variation between base colors of a pattern
        private static float COLOR_DIFF_RED = COLOR_NUMBER * 0.2f; // Rate at which color diff reduces in each iteration

        private static int? GRADIENT_COLOR_1 = null;
        private static int? GRADIENT_COLOR_2 = null;
        private static bool GRADIENT_HORIZONTAL = true;

        private static void ParseArgs(string[] args) {

            Dictionary<string, string> param = new Dictionary<string, string>();
            HashSet<string> modes = new HashSet<string>();

            for (int i = 0; i < args.Length; i++) {

                string arg = args[i];
                if (arg.StartsWith("--")) modes.Add(arg);
                else if (arg.StartsWith("-") && i + 1 < args.Length) param.Add(args[i], args[++i]);
            }

            if (param.ContainsKey("-w") && param.ContainsKey("-h"))
                IMAGE_SIZE = new Size(int.Parse(param["-w"]), int.Parse(param["-h"]));
            if (param.ContainsKey("-ps")) PIXEL_SIZE = int.Parse(param["-ps"]);
            if (param.ContainsKey("-rec")) RECURSION = int.Parse(param["-rec"]);

            if (param.ContainsKey("-cn")) COLOR_NUMBER = int.Parse(param["-cn"]);
            if (param.ContainsKey("-cd")) COLOR_DIFF = float.Parse(param["-cd"]);
            if (param.ContainsKey("-cdr")) COLOR_DIFF_RED = COLOR_NUMBER * float.Parse(param["-cdr"]);

            if (param.ContainsKey("-g1")) GRADIENT_COLOR_1 = int.Parse(param["-g1"]);
            if (param.ContainsKey("-g2")) GRADIENT_COLOR_2 = int.Parse(param["-g2"]);
            if (param.ContainsKey("-gh")) GRADIENT_HORIZONTAL = bool.Parse(param["-gh"]);
        }

        public static void Main(string[] args) {

            args = new[] {
                "-w", "1366",
                "-h", "786",
                "-ps", "8",
                "-rec", "1",
                "-cn", "16",
                "-cd", "0.2",
                "-cdr", "0.25",
                "-g1", "120",
                "-g2", "240",
                "-gh", "false"
            };

            ParseArgs(args);

            int blockSize = (int) Math.Pow(PATTERN_LENGTH, RECURSION) * PIXEL_SIZE;
            Size gridSize = new Size((IMAGE_SIZE.Width - 1) / blockSize + 1, (IMAGE_SIZE.Height - 1) / blockSize + 1);

            string filename = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) +
                "/" + DateTime.Now.Year + "-" + DateTime.Now.Month + "-" + DateTime.Now.Day +
                "-" + DateTime.Now.Hour + "-" + DateTime.Now.Minute + "-" + DateTime.Now.Second +
                "-" + DateTime.Now.Millisecond + ".png";

            Random random = new Random();

            Bitmap image = new Bitmap(IMAGE_SIZE.Width, IMAGE_SIZE.Height);

            Console.WriteLine("Algorithmically generating rock image");
            Console.WriteLine("Size: " + image.Width + "x" + image.Height + "px");
            Console.WriteLine("File: " + filename);
            Console.WriteLine("Generating: 0%");

            float totalBlocks = gridSize.Width * gridSize.Height;
            int completedBlocks = 0;
            int lastPercentage = 0;;

            bool gradient = GRADIENT_COLOR_1 != null && GRADIENT_COLOR_2 != null;
            int color1 = 0;
            int colorDiff = 0;

            if (gradient) {

                color1 = ((int) GRADIENT_COLOR_1 + 360) % 360;
                colorDiff = (int) GRADIENT_COLOR_2 - (int) GRADIENT_COLOR_1;
            }

            for (int i = 0; i < gridSize.Width; i++) {

                for (int j = 0; j < gridSize.Height; j++) {

                    float[,] block = GenerateBlock(random, random.Next(NUM_PATTERNS), blockSize);

                    for (int ci = 0; ci < blockSize; ci++) {

                        for (int cj = 0; cj < blockSize; cj++) {

                            int pi = i * blockSize + ci;
                            int pj = j * blockSize + cj;

                            if (pi < IMAGE_SIZE.Width && pj < IMAGE_SIZE.Height) {

                                if (!gradient) {

                                    int grey = (int) Math.Round(Clamp(block[ci, cj], 0, 1) * 255);
                                    image.SetPixel(pi, pj, Color.FromArgb(grey, grey, grey));
                                }
                                else {

                                    float progress;
                                    if (GRADIENT_HORIZONTAL) progress = pi / (float) IMAGE_SIZE.Width;
                                    else progress = pj / (float) IMAGE_SIZE.Height;
                                    float h = (color1 + progress * colorDiff + 360) % 360;
                                    image.SetPixel(pi, pj, ColorFromHSB(h, 0.5f, block[ci, cj]));
                                }
                            }
                        }
                    }

                    completedBlocks++;
                    int percentage = (int) (completedBlocks / totalBlocks * 100);

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
            FillSquare(rng, block, 0, 0, blockSize, 0.5f, COLOR_DIFF, patternIndex);
            return block;
        }

        private static void FillSquare(Random rng, float[,] texture,
            int initX, int initY, int length, float baseColor, float colorDiff, int patternIndex) {

            int[,] pattern = Patterns[patternIndex];
            int[] coloring = GenerateColoring(rng); // Gets a random coloring with no restrictions
            float colorVar = colorDiff / COLOR_NUMBER; // Color variation between two consecutive colors

            int cellLength = length / PATTERN_LENGTH;

            for (int i = 0; i < PATTERN_LENGTH; i++) {

                for (int j = 0; j < PATTERN_LENGTH; j++) {

                    // Gets the base color for this cell
                    float cellBaseColor = baseColor + colorDiff / 2 - colorVar * (0.5f + coloring[pattern[i, j]]);

                    if (cellLength > PIXEL_SIZE) {

                        // Fills this cell with a new pattern
                        FillSquare(rng, texture,
                            initX + i * cellLength, initY + j * cellLength, cellLength,
                            cellBaseColor, colorDiff / COLOR_DIFF_RED,
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
                coloring[i] = rng.Next(COLOR_NUMBER);

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
