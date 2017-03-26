using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Reflection;

namespace Agrock {

    public class Agrock {

        public const string ARG_WIDTH = "-w";
        public const string ARG_HEIGHT = "-h";
        public const string ARG_PIXEL_SIZE = "-ps";
        public const string ARG_RECURSION = "-rec";
        public const string ARG_GREY_NUMBER = "-gn";
        public const string ARG_GREY_BASE_DIFF = "-gbd";
        public const string ARG_GREY_DIFF_RED = "-gdr";
        public const string ARG_HUE_1 = "-h1";
        public const string ARG_HUE_2 = "-h2";
        public const string ARG_HUE_GRAD = "-hg";
        public const string ARG_SAT_1 = "-s1";
        public const string ARG_SAT_2 = "-s2";
        public const string ARG_SAT_GRAD = "-sg";
        public const string ARG_RANDOM = "--random";

        private const int NUM_PATTERNS = 36;
        private const int PATTERN_LENGTH = 4;
        private const int RECTANGLES = (PATTERN_LENGTH * PATTERN_LENGTH) / 2;

        private static readonly Random RANDOM = new Random();

        private int width = 4096;
        private int height = 2304;
        private int pixelSize = 1;
        private int recursion = 4;
        private int greyNumber = 16; // Different base greys in a pattern
        private float greyBaseDiff = 0.2f; // Maximum variation between base greys of a pattern
        private float greyDiffRed = 0.2f; // Rate at which greyBaseDiff reduces in each iteration
        private int? hue1;
        private int? hue2;
        private bool? hueGrad;
        private float? sat1;
        private float? sat2;
        private bool? satGrad;

        private readonly bool hueGradient;
        private readonly bool satGradient;
        private readonly float hueBase;
        private readonly float hueDiff;
        private readonly float satBase;
        private readonly float satDiff;

        private string getArgList() {

            return ARG_WIDTH + " " + width
                   + " " + ARG_HEIGHT + " " + height
                   + " " + ARG_PIXEL_SIZE + " " + pixelSize
                   + " " + ARG_RECURSION + " " + recursion
                   + " " + ARG_GREY_NUMBER + " " + greyNumber
                   + " " + ARG_GREY_BASE_DIFF + " " + greyBaseDiff
                   + " " + ARG_GREY_DIFF_RED + " " + greyDiffRed
                   + " " + ARG_HUE_1 + " " + hue1
                   + " " + ARG_HUE_2 + " " + hue2
                   + " " + ARG_HUE_GRAD + " " + hueGrad
                   + " " + ARG_SAT_1 + " " + sat1
                   + " " + ARG_SAT_2 + " " + sat2
                   + " " + ARG_SAT_GRAD + " " + satGrad
                   + " " + ARG_RANDOM;
        }

        public static void Main(string[] args) {

            args = new[] {

                ARG_WIDTH, "1366",
                ARG_HEIGHT, "786",
                ARG_PIXEL_SIZE, "8",
                ARG_RECURSION, "1",

                ARG_GREY_NUMBER, "16",
                ARG_GREY_BASE_DIFF, "0.2",
                ARG_GREY_DIFF_RED, "0.25",

                ARG_HUE_1, "0",
                ARG_HUE_2, "360",
                ARG_HUE_GRAD, "true",

                ARG_SAT_1, "0.0",
                ARG_SAT_2, "0.5",
                ARG_SAT_GRAD, "false",

                ARG_RANDOM
            };

            new Agrock(args);
        }

        private void ParseArgs(string[] args) {

            Dictionary<string, string> param = new Dictionary<string, string>();
            HashSet<string> modes = new HashSet<string>();

            for (int i = 0; i < args.Length; i++) {

                string arg = args[i];
                if (arg.StartsWith("--")) modes.Add(arg);
                else if (arg.StartsWith("-") && i + 1 < args.Length) param.Add(args[i], args[++i]);
            }

            if (modes.Contains(ARG_RANDOM)) SetRandomParams();

            if (param.ContainsKey(ARG_WIDTH)) width = int.Parse(param[ARG_WIDTH]);
            if (param.ContainsKey(ARG_HEIGHT)) height = int.Parse(param[ARG_HEIGHT]);
            if (param.ContainsKey(ARG_PIXEL_SIZE)) pixelSize = int.Parse(param[ARG_PIXEL_SIZE]);
            if (param.ContainsKey(ARG_RECURSION)) recursion = int.Parse(param[ARG_RECURSION]);

            if (param.ContainsKey(ARG_GREY_NUMBER)) greyNumber = int.Parse(param[ARG_GREY_NUMBER]);
            if (param.ContainsKey(ARG_GREY_BASE_DIFF)) greyBaseDiff = float.Parse(param[ARG_GREY_BASE_DIFF]);
            if (param.ContainsKey(ARG_GREY_DIFF_RED)) greyDiffRed = float.Parse(param[ARG_GREY_DIFF_RED]);

            if (param.ContainsKey(ARG_HUE_1)) hue1 = int.Parse(param[ARG_HUE_1]);
            if (param.ContainsKey(ARG_HUE_2)) hue2 = int.Parse(param[ARG_HUE_2]);
            if (param.ContainsKey(ARG_HUE_GRAD)) hueGrad = bool.Parse(param[ARG_HUE_GRAD]);

            if (param.ContainsKey(ARG_SAT_1)) sat1 = float.Parse(param[ARG_SAT_1]);
            if (param.ContainsKey(ARG_SAT_2)) sat2 = float.Parse(param[ARG_SAT_2]);
            if (param.ContainsKey(ARG_SAT_GRAD)) satGrad = bool.Parse(param[ARG_SAT_GRAD]);
        }

        private void SetRandomParams() {

            width = 600 + RANDOM.Next(6000);
            height = 400 + RANDOM.Next(4000);
            pixelSize = (int) Math.Pow(2, RANDOM.Next(5));
            recursion = 1 + RANDOM.Next(5);

            greyNumber = (int) Math.Pow(2, 1 + RANDOM.Next(5));
            greyBaseDiff = 0.1f + 0.2f * (float) RANDOM.NextDouble();
            greyDiffRed = 0.1f + 0.2f * (float) RANDOM.NextDouble();

            if (RANDOM.Next(2) == 0) hue1 = RANDOM.Next(360);
            if (RANDOM.Next(2) == 0) hue2 = RANDOM.Next(360);
            if (RANDOM.Next(2) == 0) hueGrad = RANDOM.Next(2) == 0;

            if (RANDOM.Next(2) == 0) sat1 = (float) RANDOM.NextDouble();
            if (RANDOM.Next(2) == 0) sat2 = (float) RANDOM.NextDouble();
            if (RANDOM.Next(2) == 0) satGrad = RANDOM.Next(2) == 0;
        }

        public Agrock(string[] args) {

            ParseArgs(args);

            int blockSize = (int) Math.Pow(PATTERN_LENGTH, recursion) * pixelSize;
            Size gridSize = new Size((width - 1) / blockSize + 1, (height - 1) / blockSize + 1);

            string filename = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location) +
                              "/" + DateTime.Now.Year + "-" + DateTime.Now.Month + "-" + DateTime.Now.Day +
                              "-" + DateTime.Now.Hour + "-" + DateTime.Now.Minute + "-" + DateTime.Now.Second +
                              "-" + DateTime.Now.Millisecond + ".png";

            Bitmap image = new Bitmap(width, height);

            Console.WriteLine("Args: " + getArgList());
            Console.WriteLine("Algorithmically generating rock image");
            Console.WriteLine("Size: " + image.Width + "x" + image.Height + "px");
            Console.WriteLine("File: " + filename);
            Console.WriteLine("Generating: 0%");

            float totalBlocks = gridSize.Width * gridSize.Height;
            int completedBlocks = 0;
            int lastPercentage = 0;

            hueGradient = hueGrad != null && hue1 != null && hue2 != null;
            satGradient = hue1 != null && satGrad != null && sat1 != null && sat2 != null;

            hueBase = 0;
            hueDiff = 0;
            satBase = 0;
            satDiff = 0;

            if (hueGradient) {

                hueBase = ((int) hue1 + 360) % 360;
                hueDiff = (int) hue2 - (int) hue1;
            }

            if (satGradient) {

                satBase = (float) sat1;
                satDiff = (float) sat2 - satBase;
            }

            for (int i = 0; i < gridSize.Width; i++) {

                for (int j = 0; j < gridSize.Height; j++) {

                    Color[,] block = GenerateBlock(RANDOM, RANDOM.Next(NUM_PATTERNS), i, j, blockSize);

                    for (int ci = 0; ci < blockSize; ci++) {

                        for (int cj = 0; cj < blockSize; cj++) {

                            int pi = i * blockSize + ci;
                            int pj = j * blockSize + cj;

                            if (pi < width && pj < height) image.SetPixel(pi, pj, block[ci, cj]);
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

        public Color[,] GenerateBlock(Random rng, int patternIndex, int x, int y, int size) {

            Color[,] block = new Color[size, size];
            GenerateSubBlock(rng, block, x, y, patternIndex, 0, 0, size, 0.5f, greyBaseDiff);
            return block;
        }

        private void GenerateSubBlock(Random random, Color[,] block, int blockX, int blockY, int patternIndex,
            int initX, int initY, int size, float baseGrey, float greyDiff) {

            int[,] pattern = Patterns[patternIndex];
            int[] greying = GenerateGreying(random);

            float greyVar = greyDiff / greyNumber;
            int cellLength = size / PATTERN_LENGTH;

            for (int i = 0; i < PATTERN_LENGTH; i++) {

                for (int j = 0; j < PATTERN_LENGTH; j++) {

                    float grey = baseGrey + greyDiff / 2 - greyVar * (0.5f + greying[pattern[i, j]]);

                    if (cellLength <= pixelSize) {

                        int imageInitX = blockX * block.GetLength(0) + initX;
                        int imageInitY = blockY * block.GetLength(1) + initY;
                        float[] pairLocation = GetPairLocation(pattern, i, j);

                        float hue = hue1 ?? 0f;
                        float sat = sat1 ?? 0f;

                        if (hueGradient) {

                            float progress;
                            if ((bool) hueGrad) progress = (imageInitX + pairLocation[0] * cellLength) / width;
                            else progress = (imageInitY + pairLocation[1] * cellLength) / height;
                            hue = (hueBase + progress * hueDiff + 360) % 360;
                            sat = sat1 ?? 0.5f;
                        }

                        if (satGradient) {

                            float progress;
                            if ((bool) satGrad) progress = (imageInitX + pairLocation[0] * cellLength) / width;
                            else progress = (imageInitY + pairLocation[1] * cellLength) / height;
                            sat = satBase + progress * satDiff;
                        }

                        for (int m = 0; m < cellLength; m++) {

                            for (int n = 0; n < cellLength; n++) {

                                try {

                                    block[initX + i * cellLength + m, initY + j * cellLength + n] =
                                        ColorFromHSB(hue, sat, grey);
                                }
                                catch (ArgumentException) {

                                    throw new Exception("Weird parameters");
                                }
                            }
                        }
                    }
                    else {

                        GenerateSubBlock(random, block, blockX, blockY, random.Next(NUM_PATTERNS),
                            initX + i * cellLength, initY + j * cellLength, cellLength,
                            grey, greyDiff / (greyNumber * greyDiffRed));
                    }
                }
            }
        }

        private int[] GenerateGreying(Random random) {

            int[] greying = new int[RECTANGLES];

            for (int i = 0; i < greying.Length; i++)
                greying[i] = random.Next(greyNumber);

            return greying;
        }

        public static float[] GetPairLocation(int[,] pattern, int i, int j) {

            int value = pattern[i, j];
            if (i < PATTERN_LENGTH - 1 && pattern[i + 1, j] == value) return new []{i + 0.5f, j};
            if (i > 0 && pattern[i - 1, j] == value) return new []{i - 0.5f, j};
            if (j < PATTERN_LENGTH - 1 && pattern[i, j + 1] == value) return new []{i, j + 0.5f};
            if (j > 0 && pattern[i, j - 1] == value) return new []{i, j - 0.5f};
            return null;
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
         * The 36 possible patterns (without grey randomization).
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
