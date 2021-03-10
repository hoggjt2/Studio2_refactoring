/* Program name:            Haunted House (Work in title)
 * Project file name:       HauntedHouse
 * Author:                  Steve Parker, 
 * Date:                    20/10/2020
 * Language:                C#
 * Platform:                Microsoft Visual Studio 2019
 * Purpose:                 To work in a team environment by making a text based adventure game.
 * Description:             Explore a haunted house using text commands ...
 *
 * known bugs:              
 * Additional features:     
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Media;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;

namespace HauntedHouse
{
    class Program
    {
        //this is to make the program auto full size from:
        //https://www.c-sharpcorner.com/code/448/code-to-auto-maximize-console-application-according-to-screen-width-in-c-sharp.aspx
        [DllImport("kernel32.dll", ExactSpelling = true)]
        private static extern IntPtr GetConsoleWindow();
        private static IntPtr ThisConsole = GetConsoleWindow();
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);
        private const int HIDE = 0;
        private const int MAXIMIZE = 3;
        private const int MINIMIZE = 6;
        private const int RESTORE = 9;
        private const int ANIDELAY = 500;
        private const int MAZECOUNT5 = 5;


        //Constants
        private const string FILELOCATION = @"save.txt";
        private const string MAZEANSWER = "EEWEW";

        //Fields
        private static List<Tuple<string, bool, string, string, string, string>> objects; //keeps a track of all the work the player has done in a certain room. 
        private static List<Tuple<string, int, string>> inventory; //Player's inventory
        private static List<Tuple<string, bool, string, string>> roomDirection; //direction which is allowed in each room
        private static List<string> screenSave;  //saves what is currently on the screen
        private static List<bool> roomDescription; //checks whether to describe the room or not
        private static string text;              //use this to show a message for the player
        private static int score;                //keeps a track of the player's score 
        private static bool gameStart;           //checks to see if the game has started or not
        private static bool menu;                //checks to see if the game is in the menu
        private static string playerLocation;            //the location the of the player. 
        private static int ScreenSaveCount = Console.WindowHeight - 5; //the size of the screen available for the main text.
        private static List<string> maze;       //Keeps a track of where the player went in the maze
        private static SoundPlayer soundPlayer; //plays the sounds
        private static List<string> endingTexts; //similar to screensave but does the entire screen.

        //enum for the room names
        public enum RoomNames
        {
            Spare_room = 1,
            Bedroom,
            Hallway,
            Foyer,
            Kitchen,
            Drawing_room,
            Stairs,
            Basement,
            Tunnel,
            Unknown = 12
        }

        //Main method
        static void Main(string[] args)
        {
            //Set the app to fullScreen
            Console.SetWindowSize(Console.LargestWindowWidth, Console.LargestWindowHeight);
            ShowWindow(ThisConsole, MAXIMIZE);

            //Removes the scroll bar
            Console.SetBufferSize(Console.WindowWidth, Console.WindowHeight);

            screenSave = new List<string>();
            inventory = new List<Tuple<string, int, string>>(); //name of item, how many they have on them.
            objects = new List<Tuple<string, bool, string, string, string, string>>(); //name of the location and object, have they used the object, if they activate, if they try to activate again.
            roomDirection = new List<Tuple<string, bool, string, string>>(); //name of direction, can the player go that way, the name of the location, reason why they can't go.
            roomDescription = new List<bool>();  //checks whether to show the room description or not
            maze = new List<string>(); // what position the player went in the maze
            gameStart = true;   //checks whether the game has just started
            score = 0;      //the score tally, not currently working
            playerLocation = "MainMenu"; //tells the game where the player is
            menu = true;  //checks if the player is in the menu
            endingTexts = new List<string>();
            bool exit = false; //bool to get out of the loop
            Animation(ANIDELAY, "Intro.txt");
            do
            {
                Console.Clear();
                if (!menu)
                {
                    if (CheckEnding())
                    {
                        Ending();
                    }
                    else
                    {
                        DisplayUI();
                        if (maze.Count == MAZECOUNT5)
                        {
                            MazeChecker();
                            DisplayUI();
                        }
                    }
                }
                else
                {
                    MainMenu();
                }
                //the player enters their commands here
                string selection = Console.ReadLine().ToLower();

                if (!selection.Contains("new") && !selection.Contains("save") && !selection.Contains("load") && !selection.Contains("main") && !selection.Contains("resume"))
                {
                    screenSave.Add(" > " + selection);
                }
                //splits the string into each word
                string[] playerTexts = selection.Split(" ");
                //finds what the first word does
                switch (playerTexts[0])
                {
                    case "new": //to start a new game
                        {
                            if (gameStart)
                            {
                                NewGame();
                            }
                        }
                        break;

                    case "save": //to save game
                        {
                            SaveGame();
                        }
                        break;

                    case "load": //to load game
                        {
                            if (File.Exists(FILELOCATION))
                            {
                                LoadGame();
                            }
                        }
                        break;

                    case "exit": //to quit the game
                        {
                            exit = true;
                        }
                        break;

                    case "main": //to return to the main menu
                        {
                            menu = true;
                            MainMenu();
                        }
                        break;

                    case "resume": //to resume game (bug test: if the player des this in the game, what happens?)
                        {
                            if ((!gameStart) && menu)
                            {
                                menu = false;
                            }
                            else if (!menu)
                            {
                                text = "Can't use that command here";
                                ShowMessage();
                            }
                        }
                        break;

                    case "go": //go to a direction in the game
                        {
                            Go(playerTexts);
                        }
                        break;

                    case "use": //use an item on an object
                        {
                            Use(playerTexts);
                        }
                        break;

                    case "look": //look at something, either room, object or items
                        {
                            Look(playerTexts);
                        }
                        break;

                    case "open": //open an object
                    case "take": //take object
                    case "lay": //lay object
                    case "read": //use an item on an object
                    case "move": //move the object.
                        {
                            ObjectInteraction(playerTexts);
                        }
                        break;

                    case "inventory":
                    case "i":
                        {
                            //checks each item if the player has more than 1 of the items. If they do, it will show what the item is.
                            //If they are carrying nothing, it will inform them they have nothing
                            foreach (var item in inventory)
                            {
                                int counter = 0;
                                if (item.Item2 > 0)
                                {
                                    text = item.Item1 + " " + item.Item3;
                                    ShowMessage();
                                    counter++;
                                }
                                if (counter == 0)
                                {
                                    text = "You are carrying nothing... I mean nothing at all, not even lint or a loose string.";
                                    ShowMessage();
                                }

                            }
                        }
                        break;

                    case "help": //help command
                        {
                            Help(playerTexts);
                        }
                        break;

                    default: //if nothing else
                        {
                            text = "I didn't understand that";
                            ShowMessage();
                        }
                        break;
                }
            }
            while (exit == false); //only quit the loop if they quit
        }

        static public void DisplayUI()
        {
            int quarterWidth = Console.WindowWidth / 4; // 1/4 of the console width
            int width75 = Console.WindowWidth - quarterWidth; // 75% of the console width
            string temp = "Inventory";
            int middleInv = (width75 + ((quarterWidth / 2) - (temp.Length) / 2)); //the middle where to put the word in the center of the area
            string hr = ""; //to make the horizontal ruler across the screen

            //set the cursor at the top left
            Console.SetCursorPosition(1, 0);

            //finds the room name and converts it to a string
            int roomNumber = Convert.ToInt16(playerLocation.Replace("Room", ""));
            string roomName;
            if ((roomNumber == 10) || (roomNumber == 11))
            {
                Random random = new Random();
                switch (random.Next(4))
                {
                    case 1:
                        {
                            roomName = "???";
                        }
                        break;

                    case 2:
                        {
                            roomName = "??!!?";
                        }
                        break;

                    case 3:
                        {
                            roomName = "lost?";
                        }
                        break;

                    default:
                        {
                            roomName = "Help me!!!";
                        }
                        break;
                }
            }
            else
            {
                roomName = Convert.ToString((RoomNames)roomNumber);
            }
            //If its a multiword, replace the underscore with a space
            if (roomName.Contains("_"))
            {
                roomName = roomName.Replace("_", " ");
            }
            Console.Write("Location: " + roomName); //write the location of the player

            //set the cursor to write out the score
            Console.SetCursorPosition(quarterWidth + quarterWidth / 2, 0);
            Console.Write("|| Score: " + score.ToString());

            //set the curosr to write out the inventory title
            Console.SetCursorPosition(width75, 0);
            Console.Write("||");
            Console.SetCursorPosition(middleInv, 0);
            Console.WriteLine(temp);

            //to create the horizontal line
            for (int i = 0; i < Console.WindowWidth; i++)
            {
                hr = hr + "=";
            }
            Console.WriteLine(hr); //output the line across the screen

            //to create a verticle line.
            for (int i = 2; i < Console.WindowHeight - 1; i++)
            {
                Console.SetCursorPosition(width75, i);
                Console.WriteLine("||");
            }

            //reset the horizontal ruler
            hr = "";
            //create a horizontal ruler that fits for the bottom part of the screen
            for (int i = 0; i < width75; i++)
            {
                hr = hr + "=";
            }
            Console.SetCursorPosition(0, Console.WindowHeight - 3);
            Console.WriteLine(hr);

            int count = 0;
            //Write out the items the player has on the right hand side of the screen
            foreach (var item in inventory)
            {
                if (item.Item2 > 0)
                {
                    Console.SetCursorPosition(middleInv, 3 + count);
                    Console.Write(item.Item1);
                    count++;
                }
            }
            if (count == 0)
            {
                Console.SetCursorPosition(middleInv - 2, 3 + count);
                Console.Write("You have nothing");
            }

            //resets the screen position
            Console.SetCursorPosition(0, 0);
            Console.SetCursorPosition(0, 2);

            //Cull the list to fit onto the screen
            if (screenSave.Count > ScreenSaveCount)
            {
                screenSave.RemoveRange(0, screenSave.Count - ScreenSaveCount);
            }
            foreach (string line in screenSave) //output any saved text to reload onto the screen
            {
                Console.WriteLine(line);
            }
            //these 3 lines to call the method of the location of the player
            Type type = typeof(Program);
            MethodBase method = type.GetMethod(playerLocation);
            method.Invoke(method, null);
            //set the cursor where the player types
            Console.SetCursorPosition(0, Console.WindowHeight - 2);
            Console.Write(" > ");
        }
        //Saves the game
        static public void SaveGame()
        {
            StreamWriter sw = new StreamWriter(FILELOCATION);

            sw.WriteLine(playerLocation); //saves player's location

            sw.WriteLine(inventory.Count); //saves how many items to load back in
            //saves all the items information to be able to load it back
            foreach (var item in inventory)
            {
                sw.WriteLine(item.Item1);
                sw.WriteLine(item.Item2.ToString());
                sw.WriteLine(item.Item3);
            }

            //same as above but for the objects
            sw.WriteLine(objects.Count);
            foreach (var item in objects)
            {
                sw.WriteLine(item.Item1);
                sw.WriteLine(item.Item2.ToString());
                sw.WriteLine(item.Item3);
                sw.WriteLine(item.Item4);
                sw.WriteLine(item.Item5);
                sw.WriteLine(item.Item6);
            }

            //same as above but for roomDirections
            sw.WriteLine(roomDirection.Count);
            foreach (var item in roomDirection)
            {
                sw.WriteLine(item.Item1);
                sw.WriteLine(item.Item2.ToString());
                sw.WriteLine(item.Item3);
                sw.WriteLine(item.Item4);
            }

            //same as above but for RoomDescription
            sw.WriteLine(roomDescription.Count);
            foreach (var item in roomDescription)
            {
                sw.WriteLine(item.ToString());
            }

            //removes strings if above certain count and saves each line.
            if (screenSave.Count > ScreenSaveCount)
            {
                screenSave.RemoveRange(0, screenSave.Count - ScreenSaveCount);
            }
            //saves all the screen text
            sw.WriteLine(screenSave.Count);
            foreach (string item in screenSave)
            {
                sw.WriteLine(item);
            }

            sw.Close();

            //Need to get this cleaner.
            Console.Clear();
            Console.WriteLine("Game is saved! Press any key to continue...");
            Console.ReadLine();
        }

        //Load the game 
        static public void LoadGame()
        {
            //clear the list before loading
            inventory.Clear();
            objects.Clear();
            roomDirection.Clear();
            roomDescription.Clear();
            screenSave.Clear();

            StreamReader sr = new StreamReader(FILELOCATION);

            while (!sr.EndOfStream)
            {
                playerLocation = sr.ReadLine(); //load players location

                //loads the items back to the inventory list
                int count = Convert.ToInt16(sr.ReadLine()); //load how many lines for the list
                for (int i = 0; i < count; i++)
                {
                    inventory.Add(Tuple.Create(sr.ReadLine(),
                                               Convert.ToInt32(sr.ReadLine()),
                                               sr.ReadLine()));
                }

                //same as above but for objects
                count = Convert.ToInt16(sr.ReadLine());
                for (int i = 0; i < count; i++)
                {
                    objects.Add(Tuple.Create(sr.ReadLine(),
                                             Convert.ToBoolean(sr.ReadLine()),
                                             sr.ReadLine(),
                                             sr.ReadLine(),
                                             sr.ReadLine(),
                                             sr.ReadLine()));
                }

                //same as above but for roomDirection
                count = Convert.ToInt16(sr.ReadLine());
                for (int i = 0; i < count; i++)
                {
                    roomDirection.Add(Tuple.Create(sr.ReadLine(),
                                                   Convert.ToBoolean(sr.ReadLine()),
                                                   sr.ReadLine(),
                                                   sr.ReadLine()));
                }

                //Same as above but for RoomDescription
                count = Convert.ToInt16(sr.ReadLine());
                for (int i = 0; i < count; i++)
                {
                    roomDescription.Add(Convert.ToBoolean(sr.ReadLine()));
                }

                //same as above but for screensave
                count = Convert.ToInt16(sr.ReadLine());
                for (int i = 0; i < count; i++)
                {
                    screenSave.Add(sr.ReadLine());
                }
            }
            sr.Close();

            menu = false;
        }
        //To give the player any help with commands
        static public void Help(string[] playerTexts)
        {
            if ((playerTexts.Length > 1) && (playerTexts[1] != "")) //if theres more words after the first one and isnt blank
            {
                switch (playerTexts[1])
                {
                    case "look":
                        {
                            text = "Use to look at certain objects. For example 'look chest'.";
                            ShowMessage();
                        }
                        break;

                    case "use":
                        {
                            text = "Use an item in your inventory with an object around the room. For example 'use key on door'.";
                            ShowMessage();
                        }
                        break;

                    case "go":
                        {
                            text = "Use to go in a direction. For example 'go west'";
                            ShowMessage();
                        }
                        break;

                    case "open":
                        {
                            text = "Use to open something. For example 'open chest'";
                            ShowMessage();
                        }
                        break;

                    case "take":
                        {
                            text = "Use to take an item. for example 'take key'";
                            ShowMessage();
                        }
                        break;
                    case "move":
                        {
                            text = "Use to move an item. for example 'move box'";
                            ShowMessage();
                        }
                        break;

                    default:
                        {
                            text = "No information about '" + playerTexts[1] + "'.";
                            ShowMessage();
                        }
                        break;

                }
            }
            else
            {
                text = "Current commands you can use: look, go, use, move, take, open, read, help, main, save, load, exit.";
                ShowMessage();
                text = "Type 'help [command]' for more information";
                ShowMessage();
            }
        }

        //Let the player look at something
        static public void Look(string[] playerTexts)
        {
            if ((playerTexts.Length > 1) && (playerTexts[1] != "")) //if theres more words after look and isn't blank
            {
                //gets the list of what the object is
                var objectResult = objects.Find(x => x.Item1.Contains(playerLocation + playerTexts[1]));
                //if any object contains the word the player typed, and in the same location as the player
                if (objectResult != null)
                {
                    //outputs the description string onto the screen
                    text = objectResult.Item6;
                    ShowMessage();
                }
                else //if can't find the object in question
                {
                    //let the player know the object didn't exist
                    text = "I didn't understand after " + playerTexts[0];
                    ShowMessage();
                }
            }
            else //if nothing after look, output the room description
            {
                roomDescription[Convert.ToInt16(playerLocation.Replace("Room", "")) - 1] = true;
            }
        }

        //to move the player to where they want to go (only via compass directions, not holiday resorts)
        static public void Go(string[] playerTexts)
        {
            if ((playerTexts.Length > 1) && (playerTexts[1] != "")) //if theres more words after go and isn't blank
            {
                //gets the list of what the direction is
                var direction = roomDirection.Find(x => x.Item1.Contains(playerLocation + playerTexts[1]));
                //if any direction contains the word the player typed, and in the same location as the player
                if (direction != null)
                {
                    //if the description isn't blank
                    if (direction.Item3 != "")
                    {
                        //if they are allowed to go through
                        if (direction.Item2)
                        {
                            score -= 5;
                            //change the player lcoation to the new location
                            playerLocation = direction.Item3;
                        }
                        else //if they are not allowed, show the message on why they can't
                        {
                            text = direction.Item4;
                            ShowMessage();
                        }
                    }
                    else //if blank, use the default message
                    {
                        text = "You can not go that direction.";
                        ShowMessage();
                    }
                }
                else
                {
                    text = "I didn't understand after " + playerTexts[0];
                    ShowMessage();
                }
            }
            else //if theres nothing after 'go', tell the player the message
            {
                text = "Go where?";
                ShowMessage();
            }
        }

        //let a player use an item onto an object
        static public void Use(string[] playerTexts)
        {
            if ((playerTexts.Length > 1) && (playerTexts[1] != "")) //if theres more words after item and isn't blank
            {
                //gets the list of what the item is
                var itemResult = inventory.Find(x => x.Item1.Contains(playerTexts[1]));
                //if any item contains the word the player typed, and in the same location as the player
                if (itemResult != null)
                {
                    if (itemResult.Item2 > 0)
                    {
                        //if the player typed more than more than 2 words
                        if (playerTexts.Length > 2)
                        {
                            //get the list of the object in question
                            var objectResult = objects.Find(x => x.Item1.Contains(playerLocation + playerTexts[playerTexts.Length - 1]));
                            //if theres an object the player has typed
                            if (objectResult != null)
                            {
                                //if the object matches the item
                                if (objectResult.Item5 == itemResult.Item1)
                                {
                                    //if the object is not activated
                                    if (!objectResult.Item2)
                                    {
                                        score += 20;
                                        //show the message of success and create the list to change the status from false to true;
                                        text = objectResult.Item3;
                                        ShowMessage();
                                        objects.Add(Tuple.Create(objectResult.Item1, true, objectResult.Item3, objectResult.Item4, objectResult.Item5, objectResult.Item6));
                                        objects.Remove(objectResult);
                                    }
                                    else //if the object is already activated, tell the user that it has
                                    {
                                        text = objectResult.Item4;
                                        ShowMessage();
                                    }
                                }
                                else // if the object didn't match the item, then message the player
                                {
                                    text = "It wasn't intended to be used like that.";
                                    ShowMessage();
                                }
                            }
                            else //if didn't match the object, give the message
                            {
                                text = "I didn't understand after " + playerTexts[0] + " " + playerTexts[1];
                                ShowMessage();
                            }
                        }
                        else // if th eplayer didn't type anything after the item, give the message
                        {
                            text = "use " + itemResult.Item1 + " on what?";
                            ShowMessage();
                        }
                    }
                    else //if the player doesn't have the item. give the message
                    {
                        text = "You do not have that item";
                        ShowMessage();
                    }
                }
                else
                {
                    text = "I didn't understand after " + playerTexts[0];
                    ShowMessage();
                }
            }
            else // if couldn't match the item name. message
            {
                text = "Use what?";
                ShowMessage();
            }
        }

        //let the player open something
        static public void ObjectInteraction(string[] playerTexts)
        {
            if ((playerTexts.Length > 1) && (playerTexts[1] != "")) //if theres more words after open and isn't blank
            {
                //gets the list of what the object is
                var objectResult = objects.Find(x => x.Item1.Contains(playerLocation + playerTexts[1]));
                //if any object contains the word the player typed, and in the same location as the player
                if (objectResult != null)
                {
                    //if the object has the usage of open
                    if (objectResult.Item5 == playerTexts[0])
                    {
                        //if the object hasn't already been activated
                        if (!objectResult.Item2)
                        {
                            //tell the player the success message and change the status from false to true
                            score += 10;
                            text = objectResult.Item3;
                            ShowMessage();
                            objects.Add(Tuple.Create(objectResult.Item1, true, objectResult.Item3, objectResult.Item4, objectResult.Item5, objectResult.Item6));
                            objects.Remove(objectResult);
                            if (objectResult.Item1 == "Room2jewelrybox")
                            {
                                soundPlayer = new SoundPlayer(Properties.Resources.MusicBox);
                                soundPlayer.Play();
                            }
                            var itemResult = inventory.Find(x => x.Item1.Contains(playerTexts[1]));
                            if (itemResult != null)
                            {
                                inventory.Add(Tuple.Create(itemResult.Item1, itemResult.Item2 + 1, itemResult.Item3));
                                inventory.Remove(itemResult);
                            }
                        }
                        else //if already activated, message
                        {
                            text = objectResult.Item4;
                            ShowMessage();
                        }
                    }
                    else //if the object can't be operated in that manner. message
                    {
                        text = "You can't " + playerTexts[0] + " that";
                        ShowMessage();
                    }
                }
                else //if the object didn't exist. message
                {
                    text = "I didn't understand after " + playerTexts[0];
                    ShowMessage();
                }
            }
            else //if th eplayer didn't write anything after 'open'.
            {
                text = playerTexts[0] + " what?";
                ShowMessage();
            }
        }

        static public void MainMenu()
        {
            List<string> title = new List<string>();

            //clears the text
            Console.Clear();

            //deletes the title list
            title.Clear();

            //if game has just been booted up...
            if (gameStart)
            {
                title.Add("New Game"); //add the words new game
            }
            else //otherwise...
            {
                title.Add("Resume"); //add resume and save game
                title.Add("Save Game");
            }

            //if a save file is found...
            if (File.Exists(FILELOCATION))
            {
                title.Add("Load Game"); //add load game
            }
            title.Add("Exit"); //add exit always

            Console.SetCursorPosition(0, 8); //set the cursor 8 lines down
            //Write the welcome line in the center of the console
            Console.WriteLine(String.Format("{0," +
                                           ((Console.WindowWidth / 2) +
                                           ("Welcome to Haunted House".Length / 2)) +
                                           "}", "Welcome to Haunted House"));
            Console.SetCursorPosition(0, 10); //move the cursor down 1 line
            //loop the amount of titles on the main page
            for (int i = 0; i < title.Count; i++)
            {
                Console.WriteLine("{0," +
                                 ((Console.WindowWidth / 2) +
                                 (title[i].Length / 2)) +
                                 "}", title[i]);
            }
            //set the cursor 40 characters in, and 20 lines from the top
            Console.SetCursorPosition(40, 20);
            Console.WriteLine("#"); //something to show where the player will be entering something
            Console.SetCursorPosition(41, 20); //set the cursor 1 character from the right of the above message.
        }

        //load all the items and conditions to the list
        static public void NewGame()
        {

            //declare all the inventory here.
            inventory.Add(Tuple.Create("key", 0, "It's a shiny key.... only joking, its rusted beyond believe but still works.")); //Room1 key to unlock the door in Room1.
            inventory.Add(Tuple.Create("amulet", 0, "The amulet is antique gold with large cracked emerald in the center.")); //Room1 key to unlock the door in Room1.
            inventory.Add(Tuple.Create("paper", 0, "Folded up paper under the amulet...what could be on it?")); //Room1 key to unlock the door in Room1.
            inventory.Add(Tuple.Create("candle", 0, "A burning candle."));//Room6 candle for Room4 stairs
            inventory.Add(Tuple.Create("stone", 0, "A small stone."));//Room8 stone for throwing down well
            playerLocation = "Room1"; //players starting location
            gameStart = false; //tells the game the player is now playing the game.
            menu = false; //lets the game know your not in the menu screen
            roomDescription.Add(true);//room1
            roomDescription.Add(true);//room2
            roomDescription.Add(true);//room3
            roomDescription.Add(true);//room4
            roomDescription.Add(true);//room5
            roomDescription.Add(true);//room6
            roomDescription.Add(true);//room7
            roomDescription.Add(true);//room8
            roomDescription.Add(true);//room9  Tunnel Main
            roomDescription.Add(true);//room10 Tunnel West
            roomDescription.Add(true);//room11 Tunnel East
            roomDescription.Add(true);//room12 meet the ghost scene
            Console.Clear();
        }

        //shows the player the message and saves it
        public static void ShowMessage()
        {
            int maxWidth = Console.WindowWidth - (Console.WindowWidth / 4); //the maximum width allowed
            string[] texts = text.Split(" "); //split the string into each word
            text = " "; //reset the text string to a single space.
            for (int i = 0; i < texts.Length; i++)
            {
                //if the current text plus the next word is less or equal to the maximum width allowed
                if ((text.Length + texts[i].Length + 1) <= maxWidth)
                {
                    //add the next word plus a space
                    text = text + texts[i] + " ";
                }
                else //if the maximum width has been exceeded
                {
                    //output the current text and reset the string with the current word
                    screenSave.Add(text);
                    text = " " + texts[i] + " ";
                }
            }
            //at the end, output what ever is left.
            screenSave.Add(text);
            Console.SetCursorPosition(0, 2);
            string hr = "";
            for (int i = 0; i < maxWidth; i++)
            {
                hr = hr + " ";
            }
            for (int i = 0; i < ScreenSaveCount; i++)
            {
                Console.WriteLine(hr);
            }
            Console.SetCursorPosition(0, 2);
            //Cull the list to fit onto the screen
            if (screenSave.Count > ScreenSaveCount)
            {
                screenSave.RemoveRange(0, screenSave.Count - ScreenSaveCount);
            }
            foreach (string line in screenSave) //output any saved text to reload onto the screen
            {
                Console.WriteLine(line);
            }
        }

        //This is the the method that prints the animation out to the screen. The first argument specifies the time between frames. The second specifies the file to read from
        public static void Animation(int delay, string file)
        {
            string aline;
            string filePath = Path.GetDirectoryName(System.AppDomain.CurrentDomain.BaseDirectory);
            filePath = Directory.GetParent(Directory.GetParent(filePath).FullName).FullName;
            filePath = Directory.GetParent(filePath).FullName;

            StreamReader frame = new StreamReader(filePath + "/" + @file);
            while (!frame.EndOfStream)
            {
                aline = frame.ReadLine();
                switch (aline)
                {
                    case "break":
                        {
                            Thread.Sleep(delay);
                            Console.Clear();
                        }
                        break;

                    case "fill":
                        {
                            string hr = "";
                            for (int i = 0; i < Console.WindowWidth - 1; i++)
                            {
                                hr = hr + "█";
                            }
                            for (int i = 0; i < Console.WindowHeight; i++)
                            {
                                Console.WriteLine(hr);
                            }
                        }
                        break;

                    case "thunder":
                        {
                            soundPlayer = new SoundPlayer(Properties.Resources.Thunder);
                            soundPlayer.Play();
                        }
                        break;

                    case "wait":
                        {
                            Thread.Sleep(delay);
                        }
                        break;

                    default:
                        {
                            Console.WriteLine(String.Format("{0," +
                                             ((Console.WindowWidth / 2) +
                                             (aline.Length / 2)) +
                                             "}", aline));
                        }
                        break;
                }
            }
        }

        //First room of the game, Use this as the template
        static public void Room1()
        {
            //if chest doesn't exist, create it
            if (!objects.Any(c => c.Item1.Contains("Room1chest")))
            {
                objects.Add(Tuple.Create("Room1chest", //Name of object
                                         false,        //State of the object
                                         "You opened the chest. There is a key inside", //text when first activate
                                         "the chest is already open", //text when activating the second time.
                                         "open", //What verb need to use it, (Might be able to have multiple uses, i.e. "open, move")  
                                         "Old wooden chest"));      //text describing what it is when the "player" looks at it  
            }
            //if door doesn't exist, create it
            if (!objects.Any(c => c.Item1.Contains("Room1door")))
            {
                objects.Add(Tuple.Create("Room1door",
                                         false,
                                         "you open the door",
                                         "Why would lock yourself in? You only just unlocked it!",
                                         "key",
                                         "It's a door..."));
            }

            var objectResult = objects.Find(x => x.Item1 == "Room1chest");
            //if chest is opened and key doesn't exist, create it
            if (objectResult.Item2 && (!objects.Any(c => c.Item1.Contains("Room1key"))))
            {
                objects.Add(Tuple.Create("Room1key",
                                         false,
                                         "You take the key",
                                         "You already have the key",
                                         "take",
                                         "Old brass key"));
            }
            //if direction in room1 equals 0, create all the directions
            if (roomDirection.Count(c => c.Item1.Contains("Room1")) == 0)
            {
                roomDirection.Add(Tuple.Create("Room1north", //what room this is and what direction
                                               false,        //is the player able to go this way   
                                               "",           //the name of the method it will go           
                                               ""));         //The reason they cant go this way, leave as blank if u cant go this way at all
                roomDirection.Add(Tuple.Create("Room1south",
                                               false,
                                               "",
                                               ""));
                roomDirection.Add(Tuple.Create("Room1east",
                                               false,
                                               "",
                                               ""));
                roomDirection.Add(Tuple.Create("Room1west",
                                               false,
                                               "Room2",
                                               "The door is locked, maybe there's a key somewhere in this room *shrugs*"));
            }
            //if door is open create direction west with true, and delete original
            objectResult = objects.Find(x => x.Item1 == "Room1door");
            if (objectResult.Item2)
            {
                var direction = roomDirection.Find(x => x.Item1 == "Room1west"); //finding the tuple
                roomDirection.Add(Tuple.Create(direction.Item1, true, direction.Item3, direction.Item4)); //creating a new tuple that allows to go through the door
                roomDirection.Remove(direction); //removing the old tuple
            }

            //description of the room
            if (roomDescription[0])
            {
                text = "You awaken in a dark room you do not recognize. " +
                        "You are cold and lying on the wooden floorboards in the center of the room. " +
                        "Through the window, moonlight illuminates the few objects in the room. " +
                        "An old chest lies in the corner under a thick layer of dust. " +
                        "It is as if the house has not been lived in in many years. " +
                        "There is a door to the west.";
                ShowMessage();
                roomDescription[0] = false;
            }
        }

        //First room of the game
        static public void Room2()
        {
            //if door doesn't exist, create it
            if (!objects.Any(c => c.Item1.Contains("Room2door")))
            {
                objects.Add(Tuple.Create("Room2door",
                                         false,
                                         "you open the door",
                                         "Why would lock yourself in? You only just unlocked it!",
                                         "key",
                                         "It's a door..."));
            }
            //if bed doesn't exist, create it
            if (!objects.Any(c => c.Item1.Contains("Room2bed")))
            {
                objects.Add(Tuple.Create("Room2bed", //Name of object
                                         false,        //State of the object
                                         "Old bed", //text when first activate
                                         "no, THERE IS MOULD!", //text when activating the second time.
                                         "lay", //What verb need to use it, (Might be able to have multiple uses, i.e. "open, move")  
                                         "You don’t want to lie on this bed, there is mould everywhere"));//text describing what it is when the "player" looks at it  
            }
            if (!objects.Any(c => c.Item1.Contains("Room2duchess")))
            {
                objects.Add(Tuple.Create("Room2duchess", //Name of object
                                         false,        //State of the object
                                         "Has the jewelry box on top. Go to duchess to access jewelrybox.", //text when first activate
                                         "maybe you should look in the jewelrybox", //text when activating the second time.
                                         "look", //What verb need to use it, (Might be able to have multiple uses, i.e. "open, move")  
                                         "A broken mirror above an old duchess reflects a full moon."));      //text describing what it is when the "player" looks at it  
            }
            if (!objects.Any(c => c.Item1.Contains("Room2jewelrybox")))
            {
                objects.Add(Tuple.Create("Room2jewelrybox", //Name of object
                                         false,        //State of the object
                                         "The music screeches and falters, in the box, lying on a bed of faded velvet lies an amulet with some paper folded underneath. " +
                                         "The amulet is antique gold with large cracked emerald in the center. ",//text when first activate
                                         "you've already looked in, now what?", //text when activating the second time.
                                         "open", //What verb need to use it, (Might be able to have multiple uses, i.e. "open, move")  
                                         "antique jewelrybox with creepy music"));      //text describing what it is when the "player" looks at it  
            }

            var objectResult = objects.Find(x => x.Item1 == "Room2jewelrybox");
            //if chest is opened and key doesn't exist, create it
            if (objectResult.Item2 && (!objects.Any(c => c.Item1.Contains("Room2amulet"))))
            {
                objects.Add(Tuple.Create("Room2amulet",
                                         false,
                                         "You take the amulet",
                                         "You already have the amulet",
                                         "take",
                                         "inscription of R.A.B on the back"));
            }
            if (objectResult.Item2 && (!objects.Any(c => c.Item1.Contains("Room2paper"))))
            {
                objects.Add(Tuple.Create("Room2paper",
                                         false,
                                         "Touch at your own risk… When worn this amulet allows the wearer to see those on the ‘other side’…but know this, " +
                                         "if you can see them, they can see you…If you’ve touched it, it’s too late, they know you’re here…",
                                         "You read the paper",
                                         "read",
                                         "Folded up paper under the amulet...what could be on it?"));
            }

            //if direction in room1 equals 0, create all the directions
            if (roomDirection.Count(c => c.Item1.Contains("Room2")) == 0)
            {
                roomDirection.Add(Tuple.Create("Room2north", //what room this is and what direction
                                               false,        //is the player able to go this way   
                                               "",           //the name of the method it will go           
                                               ""));         //The reason they cant go this way, leave as blank if u cant go this way at all
                roomDirection.Add(Tuple.Create("Room2south",
                                               false,
                                               "",
                                               ""));
                roomDirection.Add(Tuple.Create("Room2east",
                                               false,
                                               "",
                                               ""));
                roomDirection.Add(Tuple.Create("Room2west",
                                               true,
                                               "Room3",
                                               ""));
            }
            //if door is open create direction west with true, and delete original
            objectResult = objects.Find(x => x.Item1 == "Room2door");
            if (objectResult.Item2)
            {
                var direction = roomDirection.Find(x => x.Item1 == "Room2west"); //finding the tuple
                roomDirection.Add(Tuple.Create(direction.Item1, true, direction.Item3, direction.Item4)); //creating a new tuple that allows to go through the door
                roomDirection.Remove(direction); //removing the old tuple
            }

            //description of the room
            if (roomDescription[1])
            {
                text = "You step through the door and find yourself in an old bedroom. Thick dust is everywhere, like the first room, it has been untouched for years. " +
                    "Mold is eating away at the bed spread. The curtains hang in strips letting in a little moonlight. A broken mirror above an old duchess reflects a full moon. " +
                    "You hear strange music coming from the jewelry box upon the duchess. The door you entered from is to the east and another door is to the west. ";
                ShowMessage();
                roomDescription[1] = false;
                roomDescription[2] = true;
            }
        }

        //Hallway
        static public void Room3()
        {
            if (!objects.Any(c => c.Item1.Contains("Room3portraits")))
            {
                objects.Add(Tuple.Create("Room3portraits",
                                         false,
                                         "You can not remove them.",
                                         "You can not remove them.",
                                         "use",
                                         "All these people look haunted and miserable; " +
                                         "you feel unnerved and look away. You don’t" +
                                         " like the way their eyes follow you"));
            }

            if (!objects.Any(c => c.Item1.Contains("Room3rats")))
            {
                objects.Add(Tuple.Create("Room3rats", //Name of object
                                         false,        //State of the object
                                         "You attempt to grab one, and recieve a nast bite for your efforts", //text when first activate
                                         "That ended badly last time", //text when activating the second time.
                                         "", //What verb need to use it, (Might be able to have multiple uses, i.e. "open, move")  
                                         "The rats look diseased and sickly"));      //text describing what it is when the "player" looks at it  
            }
            if (!objects.Any(c => c.Item1.Contains("Room3stairs")))
            {
                objects.Add(Tuple.Create("Room3stairs", //Name of object
                                         false,        //State of the object
                                         "You look down the stairs", //text when first activate
                                         "You look down the stairs", //text when activating the second time.
                                         "use", //What verb need to use it, (Might be able to have multiple uses, i.e. "open, move")  
                                         "The stairs look rickety but should hold your weight"));      //text describing what it is when the "player" looks at it  
            }

            //if direction in room1 equals 0, create all the directions
            if (roomDirection.Count(c => c.Item1.Contains("Room3")) == 0)
            {
                roomDirection.Add(Tuple.Create("Room3north", //what room this is and what direction
                                               false,        //is the player able to go this way   
                                               "SOMETHING OBSCURE",           //the name of the method it will go           
                                               "You peek inside. Nothing but broken furniture and rats, a large dark stain in the " +
                                               "center. Something awful happened in this room. You can feel it, you back out immediately."));         //The reason they cant go this way, leave as blank if u cant go this way at all
                roomDirection.Add(Tuple.Create("Room3south",
                                               true,
                                               "Room2",
                                               ""));
                roomDirection.Add(Tuple.Create("Room3east",
                                               false,
                                               "",
                                               ""));
                roomDirection.Add(Tuple.Create("Room3west",
                                               true,
                                               "Room4",
                                               ""));
            }
            //description of the room
            if (roomDescription[2])
            {
                text = "You enter the hallway; you are on the " +
                    "top floor of what looks like a two story " +
                    "house long abandoned by previous tenants." +
                    " Faded portraits from the Victorian era line " +
                    "the walls. You see another room at the north end of " +
                    "the hallway, the door is hanging off the hinges. " +
                    "To the west, stairs lead down to the ground floor.";
                ShowMessage();
                roomDescription[2] = false;
                roomDescription[1] = true;
            }

        }

        //Foyer
        static public void Room4()
        {

            //Objects
            if (!objects.Any(c => c.Item1.Contains("Room4coat")))
            {
                objects.Add(Tuple.Create("Room4coat",
                                         false,
                                         "You move the coat aside to reveal a hidden door.",
                                         "You return the coat to the rack",
                                         "move", //should be move
                                         "The coats is old and worn. They smell of age and decay. Strangely you feel a slight breeze as you near it."));
            }

            //Object status checks
            //Move the coat to reveal a door
            var objectResult = objects.Find(x => x.Item1 == "Room4coat");
            if (objectResult.Item2 && (!objects.Any(c => c.Item1.Contains("Room4door"))))
            {
                objects.Add(Tuple.Create("Room4door",
                                         false,
                                         "You open the door and look down a staircase",
                                         "You open the door and look down a staircase",
                                         "open",
                                         "It appears to lead to a basement"));
            }
            //Return door to closed
            if (objectResult.Item2 && (objects.Any(c => c.Item1.Contains("Room4door"))) && (roomDescription[3] == true))
            {
                var doorState = objects.Find(x => x.Item1 == "Room4door");
                objects.Add(Tuple.Create("Room4door",
                                         false,
                                         "You open the door and see a dark staircase",
                                         "You open the door and see a dark staircase",
                                         "open",
                                         "It appears to lead to a basement"));
                objects.Remove(doorState);
            }
            //Open the door to descend the staircase
            if (objectResult.Item2 && (objects.Any(c => c.Item1.Contains("Room4door"))))
            {
                objectResult = objects.Find(x => x.Item1 == "Room4door");
                if (objectResult.Item2)
                {
                    roomDirection.Add(Tuple.Create("Room4down",
                                               true,
                                               "Room7",
                                               ""));
                }
            }

            //Directions
            if (roomDirection.Count(c => c.Item1.Contains("Room4")) == 0)
            {
                roomDirection.Add(Tuple.Create("Room4north", //what room this is and what direction
                                               false,        //is the player able to go this way   
                                               "",           //the name of the method it will go           
                                               ""));         //The reason they cant go this way, leave as blank if u cant go this way at all
                roomDirection.Add(Tuple.Create("Room4south",
                                               true,
                                               "Room3",
                                               ""));
                roomDirection.Add(Tuple.Create("Room4east",
                                               true,
                                               "Room5",
                                               ""));
                roomDirection.Add(Tuple.Create("Room4west",
                                               true,
                                               "Room6",
                                               ""));
            }
            //description of the room
            if (roomDescription[3])
            {
                text = "You walk down slowly, some of these stairs look like they " +
                    "won’t hold your weight, as you near the bottom, footsteps sound" +
                    " at the top. You turn around, no one is there. Is it your imagination?" +
                    " The stairs feel like they are never ending despite there only being 15" +
                    " or so. You finally reach the bottom. By this time, you have broken out in" +
                    " a cold sweat, every step you took, another followed, only shadows were" +
                    " there when you looked back. You are facing the front door; the kitchen lies to" +
                    " the east, a drawing room to the west. A rack beside the bottom of the stairs holds a moth eaten fur coat";
                ShowMessage();
                roomDescription[3] = false;
                roomDescription[2] = true;
                roomDescription[4] = true;
                roomDescription[5] = true;
                roomDescription[6] = true;
            }

        }

        //Kitchen
        static public void Room5()
        {
            //Objects
            if (!objects.Any(c => c.Item1.Contains("Room5rats")))
            {
                objects.Add(Tuple.Create("Room5rats",
                                         false,
                                         "That was.......messy.",
                                         "Please refrain from mutilating the local fauna.",
                                         "open",
                                         "The rats crawl over the bench and tables. " +
                                         "They seem unconcerned by your prescence."));
            }


            //Directions
            if (roomDirection.Count(c => c.Item1.Contains("Room5")) == 0)
            {
                roomDirection.Add(Tuple.Create("Room5north", //what room this is and what direction
                                               false,        //is the player able to go this way   
                                               "",           //the name of the method it will go           
                                               ""));         //The reason they cant go this way, leave as blank if u cant go this way at all
                roomDirection.Add(Tuple.Create("Room5south",
                                               false,
                                               "",
                                               ""));
                roomDirection.Add(Tuple.Create("Room5east",
                                               false,
                                               "",
                                               ""));
                roomDirection.Add(Tuple.Create("Room5west",
                                               true,
                                               "Room4",
                                               ""));
            }
            //description of the room
            if (roomDescription[4])
            {
                text = "More rats. A door that looks like it might lead to the backyard is across the room";
                ShowMessage();
                roomDescription[3] = true;
                roomDescription[4] = false;
            }

        }

        //Drawing Room
        static public void Room6()
        {
            //Objects
            if (!objects.Any(c => c.Item1.Contains("Room6candle")))
            {
                objects.Add(Tuple.Create("Room6candle",
                                         false,
                                         "you take the candle",
                                         "",
                                         "take",
                                         "The candle burns brightly in the otherwise dim room."));
            }

            //Directions
            if (roomDirection.Count(c => c.Item1.Contains("Room6")) == 0)
            {
                roomDirection.Add(Tuple.Create("Room6north", //what room this is and what direction
                                               false,        //is the player able to go this way   
                                               "",           //the name of the method it will go           
                                               ""));         //The reason they cant go this way, leave as blank if u cant go this way at all
                roomDirection.Add(Tuple.Create("Room6south",
                                               false,
                                               "",
                                               ""));
                roomDirection.Add(Tuple.Create("Room6east",
                                               true,
                                               "Room4",
                                               ""));
                roomDirection.Add(Tuple.Create("Room6west",
                                               false,
                                               "",
                                               ""));
            }
            //description of the room
            if (roomDescription[5])
            {
                text = "A couch and two chairs surround a mahogany table with intricately " +
                    "carved with legs displaying flowers and vines. This lounge set would fetch " +
                    "thousands in an antique’s auction, if only it had been properly stored. Mold " +
                    "has eaten into what once must have been plush red velvet. On the coffee table " +
                    "lies a candle holder with a fresh candle. No other rooms lead off this room.";
                ShowMessage();
                roomDescription[3] = true;
                roomDescription[5] = false;
            }

        }

        //Stairs
        static public void Room7()
        {
            //Objects
            if (!objects.Any(c => c.Item1.Contains("Room7stairs")))
            {
                objects.Add(Tuple.Create("Room7stairs", //Name of object
                                         false,        //State of the object
                                         "You may now descend the stairs aided by your trusty everlasting candle.", //text when first activate
                                         "You can safely descend the stairs.", //text when activating the second time.
                                         "candle", //What verb need to use it, (Might be able to have multiple uses, i.e. "open, move")  
                                         "The stairwell is pitch black. To descent could be dangerous."));      //text describing what it is when the "player" looks at it  
            }

            //Directions
            if (roomDirection.Count(c => c.Item1.Contains("Room7")) == 0)
            {
                roomDirection.Add(Tuple.Create("Room7north", //what room this is and what direction
                                               true,        //is the player able to go this way   
                                               "Room99",           //the name of the method it will go           
                                               ""));         //The reason they cant go this way, leave as blank if u cant go this way at all
                roomDirection.Add(Tuple.Create("Room7south",
                                               true,
                                               "Room4",
                                               ""));
                roomDirection.Add(Tuple.Create("Room7east",
                                               false,
                                               "",
                                               ""));
                roomDirection.Add(Tuple.Create("Room7west",
                                               false,
                                               "",
                                               ""));
            }

            //If candle used on stairs
            var objectResult = objects.Find(x => x.Item1 == "Room7stairs");
            if (objectResult.Item2)
            {
                var direction = roomDirection.Find(x => x.Item1 == "Room7north"); //finding the tuple
                roomDirection.Add(Tuple.Create(direction.Item1, direction.Item2, "Room8", direction.Item4)); //creating a new tuple that allows to go through the door
                roomDirection.Remove(direction); //removing the old tuple
            }

            //description of the room
            if (roomDescription[6])
            {
                text = "As you descend the stairs you are engulfed in darkness. To continue further could prove dangerous";
                ShowMessage();
                roomDescription[3] = true;
                roomDescription[6] = false;
                roomDescription[7] = true;
            }

        }

        //Passage
        public static void Room8()
        {
            //Objects
            if (!objects.Any(c => c.Item1.Contains("Room8dolls")))
            {
                objects.Add(Tuple.Create("Room8dolls", //Name of object
                                         false,        //State of the object
                                         "", //text when first activate
                                         "dolls activated", //text when activating the second time.
                                         "", //What verb need to use it, (Might be able to have multiple uses, i.e. "open, move")  
                                         "You move towards the dolls, as you take your second step, one slips of the pile and crashes to the floor, further shattering it’s already damaged face, you look back at the pile and you swear that some of expressions changed… You back away slowly"));      //text describing what it is when the "player" looks at it  
            }
            if (!objects.Any(c => c.Item1.Contains("Room8trapdoor")))
            {
                objects.Add(Tuple.Create("Room8trapdoor", //Name of object
                                         false,        //State of the object
                                         "", //text when first activate
                                         "", //text when activating the second time.
                                         "", //What verb need to use it, (Might be able to have multiple uses, i.e. "open, move")  
                                         "The trap door is locked shut with chains well rusted over the years."));      //text describing what it is when the "player" looks at it  
            }
            if (!objects.Any(c => c.Item1.Contains("Room8bookcase")))
            {
                objects.Add(Tuple.Create("Room8bookcase", //Name of object
                                         false,        //State of the object
                                         "With some effort the bookcase swings aside to reveal a stone tunnel.", //text when first activate
                                         "", //text when activating the second time.
                                         "open", //What verb need to use it, (Might be able to have multiple uses, i.e. "open, move")  
                                         "There is nothing on the shelves, but you do notice that the bookcase is sitting at an odd angle, not quite flush with the wall. You feel a slight breeze."));      //text describing what it is when the "player" looks at it  
            }
            if (!objects.Any(c => c.Item1.Contains("Room8stone")))
            {
                objects.Add(Tuple.Create("Room8stone",
                                         false,
                                         "",
                                         "",
                                         "take",
                                         "It is a small stone."));
            }

            //Directions
            if (roomDirection.Count(c => c.Item1.Contains("Room8")) == 0)
            {
                roomDirection.Add(Tuple.Create("Room8north", //what room this is and what direction
                                               false,        //is the player able to go this way   
                                               "",           //the name of the method it will go           
                                               ""));         //The reason they cant go this way, leave as blank if u cant go this way at all
                roomDirection.Add(Tuple.Create("Room8south",
                                               true,
                                               "Room7",
                                               ""));
                roomDirection.Add(Tuple.Create("Room8east",
                                               false,
                                               "Room9",
                                               ""));
                roomDirection.Add(Tuple.Create("Room8west",
                                               false,
                                               "",
                                               ""));
            }

            //if door is open create direction west with true, and delete original
            var objectResult = objects.Find(x => x.Item1 == "Room8bookcase");
            if (objectResult.Item2)
            {
                var direction = roomDirection.Find(x => x.Item1 == "Room8east"); //finding the tuple
                roomDirection.Add(Tuple.Create(direction.Item1, true, direction.Item3, direction.Item4)); //creating a new tuple that allows to go through the door
                roomDirection.Remove(direction); //removing the old tuple
            }

            //Room description
            if (roomDescription[7])
            {
                text = "You make it down the stairs without falling; if " +
                    "it weren’t for the candle you likely would have " +
                    "broken your back. An old armchair sits in one of the " +
                    "corners piled high with porcelain dolls with cracked " +
                    "and peeling faces. A huge weighted trapdoor sits over " +
                    "a round cement well in the center of the room. A small " +
                    "stone sits beside the well. In another corner there is a " +
                    "painting of what was once a beautiful young woman, she appears sad in the painting. " +
                    "To the east, there is a bookcase.";
                ShowMessage();
                roomDescription[6] = true;
                roomDescription[7] = false;
            }
        }

        //If player uses room7 stairs without candle
        public static void Room99()
        {
            text = "Death by stairs!";
            ShowMessage();
        }

        //tunnel main
        public static void Room9()
        {
            
            //if direction in room1 equals 0, create all the directions
            if (roomDirection.Count(c => c.Item1.Contains("Room9")) == 0)
            {
                roomDirection.Add(Tuple.Create("Room9north", //what room this is and what direction
                                               false,        //is the player able to go this way   
                                               "",           //the name of the method it will go           
                                               ""));         //The reason they cant go this way, leave as blank if u cant go this way at all
                roomDirection.Add(Tuple.Create("Room9south",
                                               false,
                                               "",
                                               ""));
                roomDirection.Add(Tuple.Create("Room9east",
                                               true,
                                               "Room11",
                                               ""));
                roomDirection.Add(Tuple.Create("Room9west",
                                               true,
                                               "Room10",
                                               ""));
            }

            //description of the room
            if (roomDescription[8])
            {
                text = "You can only go east or west";
                ShowMessage();
                roomDescription[8] = false;
            }
        }

        //tunnel west
        public static void Room10()
        {

            //if direction in room1 equals 0, create all the directions
            if (roomDirection.Count(c => c.Item1.Contains("Room10")) == 0)
            {
                roomDirection.Add(Tuple.Create("Room10north", //what room this is and what direction
                                               false,        //is the player able to go this way   
                                               "",           //the name of the method it will go           
                                               ""));         //The reason they cant go this way, leave as blank if u cant go this way at all
                roomDirection.Add(Tuple.Create("Room10south",
                                               false,
                                               "",
                                               ""));
                roomDirection.Add(Tuple.Create("Room10east",
                                               true,
                                               "Room11",
                                               ""));
                roomDirection.Add(Tuple.Create("Room10west",
                                               true,
                                               "Room10",
                                               ""));
            }

            //description of the room
            if (roomDescription[9])
            {
                text = "west tunnel";
                ShowMessage();
                roomDescription[9] = false;
            }

            maze.Add("W");

        }

        //tunnel east
        public static void Room11()
        {

            //if direction in room1 equals 0, create all the directions
            if (roomDirection.Count(c => c.Item1.Contains("Room11")) == 0)
            {
                roomDirection.Add(Tuple.Create("Room11north", //what room this is and what direction
                                               false,        //is the player able to go this way   
                                               "",           //the name of the method it will go           
                                               ""));         //The reason they cant go this way, leave as blank if u cant go this way at all
                roomDirection.Add(Tuple.Create("Room11south",
                                               false,
                                               "",
                                               ""));
                roomDirection.Add(Tuple.Create("Room11east",
                                               true,
                                               "Room11",
                                               ""));
                roomDirection.Add(Tuple.Create("Room11west",
                                               true,
                                               "Room10",
                                               ""));
            }

            //description of the room
            if (roomDescription[10])
            {
                text = "east tunnel";
                ShowMessage();
                roomDescription[10] = false;
            }

            maze.Add("E");
        }

        public static void MazeChecker()
        {
            string wordCheck = "";

            foreach (string word in maze)
            {
                wordCheck += word;
            }

            if (wordCheck == MAZEANSWER)
            {
                playerLocation = "Room12";
            }
            else
            {
                playerLocation = "Room9";
            }
            maze.Clear();
        }

        public static void Room12()
        {
            if (!objects.Any(c => c.Item1.Contains("Room12ghost")))
            {
                objects.Add(Tuple.Create("Room12ghost", //Name of object
                                         false,        //State of the object
                                         "", //text when first activate
                                         "", //text when activating the second time.
                                         "amulet", //What verb need to use it, (Might be able to have multiple uses, i.e. "open, move")  
                                         "You see a ghost in front of you. She seems to be looking for something."));      //text describing what it is when the "player" looks at it  
            }



            if (roomDescription[11])
            {
                text = "You come to a room and can see the base of the well in the center, " +
                    "an ethereal barely visible wispy shape forms in front of you. " +
                    "The longer you look the further the details of the shape develop. " +
                    "You find yourself face to face with the beautiful young woman from the portrait. " +
                    "You gasp at her beauty and the sadness in her eyes.";
                ShowMessage();
                roomDescription[11] = false;
            }
        }
        
        public static bool CheckEnding()
        {
            bool end = false;
            var objectResult = objects.Find(x => x.Item1.Contains("Room12ghost"));

            if (objectResult != null)
            {
                if (objectResult.Item2)
                {
                    end = true;
                }
            }
            return end;
        }


        public static void Ending()
        {
            text = "“My name is Rose Abigail Black; thank you for finding me. " +
                "I have waited so long. " +
                "Almost 200 years ago this was my home, I was happy… " +
                "My father said it was time I was married and that he had arranged for it to be so with the " +
                "son of another affluent family in town. " +
                "His name was Henry H. Holmes, he was so handsome. I was thrilled with the match… ";

            EditEndingText();

            text = "Then it all started to go wrong, " +
                "behind his keen intellect and charming personality lay the soul of a monster. " +
                "He treated me most violently, " +
                "I was desperate to get away before the marriage took place and before being taken from my home and left in his care. " +
                "I went to my father, I showed him the marks left upon my pale skin from his latest attentions. " +
                "At once, my father called him to the house and declared that the marriage would no longer take place. " +
                "Henry was furious, he stormed from the house. " +
                "I breathed a sigh of relief, believing myself finally safe. ";

            EditEndingText();

            text = "That night he returned. " +
                "With a knife taken from the kitchen he bounded up the stairs to my parent’s bedroom and brutally murdered them. " +
                "Next, he entered my room, bound my wrists, stabbed me once in the stomach and dragged me down the stairs. " +
                "Even the maids in the drawing room were not spared as they were found hiding from the commotion behind the couch. ";

            EditEndingText();

            text = "I was dragged down more stairs to the basement. " +
                "He snatched the locket from my neck and threw me down the old well in which the house had been built over. " +
                "He locked me inside. There I splashed and screamed until the loss of blood finally drained away my life force. " +
                "Here I have waited for someone to find me. ";

            EditEndingText();

            text = "Henry haunts the upper levels of this house, his soul not pure enough to pass on, " +
                "his only joy is knowing I am trapped here with him. " +
                "I have waited for someone to hear my story and return to me my most precious possession, " +
                "the amulet given to me at birth by my father, he said it would protect me. " +
                "Only by wearing it again can I leave this place.";

            EditEndingText();

            ShowEnding();
            menu = true;
        }


        public static void EditEndingText()
        {
            int maxWidth = Console.WindowWidth; //the maximum width allowed
            string[] texts = text.Split(" "); //split the string into each word
            text = " "; //reset the text string to a single space.
            for (int i = 0; i < texts.Length; i++)
            {
                //if the current text plus the next word is less or equal to the maximum width allowed
                if ((text.Length + texts[i].Length + 1) < maxWidth)
                {
                    //add the next word plus a space
                    text = text + texts[i] + " ";
                }
                else //if the maximum width has been exceeded
                {
                    //output the current text and reset the string with the current word
                    endingTexts.Add(text);
                    text = " " + texts[i] + " ";
                }
            }
            //at the end, output what ever is left.
            endingTexts.Add(text);
            endingTexts.Add("Break");
        }

        public static void ShowEnding()
        {

            Console.Clear();

            foreach (string line in endingTexts) //output any saved text to reload onto the screen
            {
                if (line == "Break")
                {
                    Console.WriteLine();
                    Thread.Sleep(6000);
                }
                else
                {
                    Console.WriteLine(line);
                }
            }

            Console.WriteLine("Press any key to go back to the main menu");
        }
    }

}


