using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;

namespace TranslateRedirectedResources
{
    class TranslateRedirectedResources
    {
        static void Main(string[] args)
        {
            var rootFolder = Path.Combine(Environment.CurrentDirectory, "GameData/BepInEx/Translation");
            if (!Directory.Exists(rootFolder))
            {
                // Allow running directly on the IO_Translation repo
                rootFolder = Path.Combine(Environment.CurrentDirectory, "Translation");
                if (!Directory.Exists(rootFolder))
                {
                    Console.WriteLine("Cold not find the Translation folder! Place this next to the GameData or the Translation folder.");
                    Console.ReadLine();
                    return;
                }
            }

            string inputFolder = Path.Combine(rootFolder, "en/Text");
            string translatedFileName;
            string outputFolder = Path.Combine(rootFolder, "en/RedirectedResources/assets/io_data/data/masterscenario.unity3d/");
            string dumpFolder = Path.Combine(rootFolder, "en/RedirectedResources/assets/io_data/data/OriginalDump");

            Dictionary<string, string> TranslationsDictionary = new Dictionary<string, string>();
            string key = "";
            string value;

            string[] fileNames = Directory.GetFiles(dumpFolder);

            string[] inputFileNames = Directory.GetFiles(inputFolder);

            //=========================== Populating Translation Dictionary ==========================================
            for (int i = 0; i < inputFileNames.Length; i++)
            {
                translatedFileName = inputFileNames[i];
                if (!translatedFileName.StartsWith("_"))
                {
                    string[] translatedFile = File.ReadAllLines(translatedFileName);
                    foreach (string line in translatedFile)
                    {
                        if (!line.StartsWith("//"))
                        {
                            string[] parts = line.Split('=');
                            if (parts.Length == 2)
                            {
                                key = parts[0].Replace("\\n　", "");
                                key = key.Replace("\\n ", "");
                                key = key.Replace("\\n", "");
                                value = parts[1];
                                if (!TranslationsDictionary.ContainsKey(key))
                                    TranslationsDictionary.Add(key, value);
                            }
                        }
                    }
                }
            }



            //======================================== Translating Dumped Files ============================================
            for (int fileIndex = 0; fileIndex < fileNames.Length; fileIndex++)
            {

                string thisFile = fileNames[fileIndex];
                string[] dumpedFile = File.ReadAllLines(thisFile);
                string[] outputFile = new string[dumpedFile.Length];

                //If is inside a group of lines, this number is positive
                int groupLenght = 0;

                int currentIndex = 0;
                int nextIndex;

                string thisLine;
                string nextLine;
                string translatedLine;

                for (int dumpPos = 0; dumpPos < dumpedFile.Length - 1; dumpPos++)
                {
                    thisLine = dumpedFile[dumpPos];
                    nextLine = dumpedFile[dumpPos + 1];

                    //As "fileIndex" advances, the number of remaining lines in a group of lines decreases
                    groupLenght--;

                    //Check for useless lines
                    if (!thisLine.Contains("CH") &&
                        !thisLine.TrimStart().StartsWith("***") &&
                        !thisLine.Contains("//") &&
                        thisLine != "主人公" &&
                        thisLine != "音瑚" &&
                        thisLine != "兎萌" &&
                        thisLine != "スタッフＡ" &&
                        thisLine != "スタッフＢ" &&
                        thisLine != "スタッフ" &&
                        thisLine != "兎萌の夫" &&
                        thisLine != "カップル・男" &&
                        thisLine != "カップル・女" &&
                        thisLine != "カップル女" &&
                        thisLine != "客１" &&
                        thisLine != "客２" &&
                        thisLine != "客３" &&
                        thisLine != "客４" &&
                        thisLine != "客５" &&
                        thisLine != "男１" &&
                        thisLine != "男２" &&
                        thisLine != "男３" &&
                        thisLine != "" &&
                        groupLenght <= 0)
                    {
                        //================= Making the key =================
                        key = thisLine;

                        // Treating Group of Lines
                        // Making the key the same as translated file, and also defining the lenght of the group of lines
                        if (thisLine != "" && nextLine != "" && !nextLine.Contains("//"))
                        {
                            groupLenght = 1;
                            bool groupEnd = false;
                            while (!groupEnd)
                            {
                                nextIndex = dumpPos + groupLenght;
                                //key = key + "\\n" + dumpedFile[nextIndex];
                                key = key + (dumpedFile[nextIndex].TrimStart('　')).TrimStart(' ');

                                //groupLenght increases until the end of consecutive valid lines
                                groupLenght++;

                                nextIndex = dumpPos + groupLenght;
                                if (dumpedFile[nextIndex] == "" || dumpedFile[nextIndex].StartsWith("//"))
                                    groupEnd = true;
                            }
                        }

                        key = key.Replace("「", "『");
                        key = key.Replace("」", "』");

                        //Translating lines found with the key
                        if (TranslationsDictionary.ContainsKey(key))
                        {
                            //Splitting lines in separated words
                            string originalTranslation = TranslationsDictionary[key];
                            string[] words = originalTranslation.Split(' ');
                            int numWords = words.Length;

                            // making a new line if lenght exceeds is maximum
                            translatedLine = "";
                            int maxLenght = 50;
                            int currentLenght = 0;
                            int currentLine = 1;

                            //Miconisomi only allows up to 3 lines per screen
                            float lineNumber = (float)originalTranslation.Length / (float)maxLenght;
                            //if there's more than 3 lines, split the chars in these 3 lines
                            if (lineNumber > 3f)
                                maxLenght = originalTranslation.Length / 3;

                            for (int wordIndex = 0; wordIndex < numWords; wordIndex++)
                            {
                                if ((currentLenght + words[wordIndex].Length) > maxLenght && currentLine < 3)
                                {
                                    translatedLine = translatedLine + "\n";
                                    currentLenght = 0;
                                    currentLine++;
                                }

                                translatedLine = translatedLine + words[wordIndex] + " ";
                                currentLenght += words[wordIndex].Length + 1;
                            }

                            translatedLine = PostTranslation(dumpedFile[dumpPos], translatedLine);
                            outputFile[currentIndex] = translatedLine;
                            currentIndex++;
                        }
                        //if current line was not found in the dictionary, just copy
                        else
                        {
                            outputFile[currentIndex] = dumpedFile[dumpPos];
                            currentIndex++;
                        }
                    }
                    //if current line is not a useful line, just copy
                    else
                    {
                        if (groupLenght <= 0 || !TranslationsDictionary.ContainsKey(key))
                        {
                            outputFile[currentIndex] = dumpedFile[dumpPos];
                            currentIndex++;
                        }
                    }
                }

                //Copying the last line
                outputFile[currentIndex] = dumpedFile[dumpedFile.Length - 1];



                //Finally, writing the file!
                string filePath = outputFolder + Path.GetFileName(thisFile);
                File.WriteAllText(filePath, string.Join("\n", outputFile));
            }

            //Exit dialogue
            Console.WriteLine("Done, press enter to exit");
            Console.ReadLine();
        }


        static string PostTranslation(string dump, string translation)
        {
            dump = dump.Replace("「", "『");
            dump = dump.Replace("」", "』");
            translation = translation.TrimEnd(' ');

            if (dump.Contains("『"))
            {
                if (translation.StartsWith("\""))
                    translation = translation.TrimStart('"');
                translation = "『" + translation;
                if (translation.EndsWith("\""))
                    translation = translation.TrimEnd('"');
                if (translation.EndsWith("”"))
                    translation = translation.TrimEnd('”');
                translation = translation + "』";
            }

            return translation;
        }
    }
}
