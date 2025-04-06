using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.IO;

class Program
{
    static void Main()
    {
        Game game = new Game();
        game.Start();
    }
}

class Room
{
    public string Name { get; }
    public string Description { get; set; }
    public Dictionary<string, Room> Exits { get; } = new Dictionary<string, Room>();
    public List<Item> Items { get; } = new List<Item>();

    public Room(string name, string description)
    {
        Name = name;
        Description = description;
    }

    public void Describe()
    {
        Console.WriteLine($"{Name}: {Description}");
        if (Items.Count > 0)
        {
            Console.WriteLine("You see:");
            foreach (var item in Items)
            {
                Console.WriteLine($"- {item.Name}");
            }
        }
        if (Exits.Count > 0)
        {
            Console.WriteLine("Exits:");
            foreach (var direction in Exits.Keys)
            {
                Console.WriteLine($"- {direction}");
            }
        }
    }
}

class Item
{
    public string Name { get; }
    public string Description { get; }
    public Action<Game> UseAction { get; }

    public Item(string name, string description, Action<Game> useAction = null)
    {
        Name = name;
        Description = description;
        UseAction = useAction;
    }

    public void Inspect()
    {
        Console.WriteLine(Description);
    }

    public void Use(Game game)
    {
        UseAction?.Invoke(game);
    }
}

class GameState
{
    public string CurrentRoomName { get; set; }
    public List<string> InventoryItemNames { get; set; }
    public Dictionary<string, List<string>> RoomInventories { get; set; }
    public bool LeverPulled { get; set; }
    public bool ChestOpened { get; set; }
}

class Game
{
    public Room currentRoom;
    public Dictionary<string, Room> rooms = new Dictionary<string, Room>();
    public List<Item> inventory = new List<Item>();
    private const int InventoryLimit = 10;
    public bool leverPulled = false;
    public bool chestOpened = false;
    private const string SaveFileName = "savegame.json";

    public Game()
    {
        Room foyer = new Room("Foyer", "A dimly lit entrance hall with a dusty chandelier.");
        Room library = new Room("Library", "Shelves filled with ancient books cover the walls.");
        Room diningHall = new Room("Dining Hall", "A long table sits beneath a cracked stained-glass window. There is a locked chest here.");
        Room secretRoom = new Room("Secret Room", "A hidden room filled with secrets of the mansion's past.");
        rooms["Secret Room"] = secretRoom;

        Item lever = new Item("Lever", "A small lever hidden behind the globe. It looks like it can be pulled.", (game) =>
        {
            if (!game.leverPulled)
            {
                game.leverPulled = true;
                library.Exits["east"] = secretRoom;
                Console.WriteLine("You pull the lever. You hear a grinding sound as a hidden door opens to the east!");
            }
            else
            {
                Console.WriteLine("The lever has already been pulled.");
            }
        });

        Item oldScroll = new Item("Old Scroll", "An ancient scroll detailing the history of the mansion's reclusive former owner.");
        secretRoom.Items.Add(oldScroll);

        foyer.Items.AddRange(new List<Item>
        {
            new Item("Umbrella", "A black umbrella, slightly damp."),
            new Item("Coat Rack", "An ornate wooden rack with a single coat hanging."),
            new Item("Welcome Mat", "A mat that says 'WELCOME' in faded letters."),
            new Item("Dusty Mirror", "A tall mirror covered in a layer of dust."),
            new Item("Old Key", "A small brass key with intricate carvings."),
            new Item("Candle", "A candle that has almost melted down to the base."),
            new Item("Letter", "A sealed letter addressed to no one in particular."),
            new Item("Hat", "A stylish fedora that has seen better days."),
            new Item("Boots", "A pair of muddy boots."),
            new Item("Painting", "A portrait of a stern-looking gentleman."),
            new Item("Vase", "A cracked vase with dried flowers.")
        });

        library.Items.AddRange(new List<Item>
        {
            new Item("Ancient Book", "An old leather-bound book with strange symbols on its cover."),
            new Item("Magnifying Glass", "A brass-handled magnifying glass."),
            new Item("Reading Lamp", "A small green-shaded lamp flickering slightly."),
            new Item("Scroll", "A rolled-up parchment with illegible text."),
            new Item("Feather Pen", "A black feathered pen with a silver tip."),
            new Item("Notebook", "A notebook filled with cryptic notes."),
            new Item("Globe", "A dusty globe, slightly faded. Behind it, you notice a lever. Try inspecting or using the lever."),
            lever,
            new Item("Telescope", "A short telescope aimed at the ceiling."),
            new Item("Bookshelf", "A massive bookshelf, some books seem loose."),
            new Item("Ink Bottle", "A half-full bottle of blue ink."),
            new Item("Rug", "A finely woven rug, frayed at the edges.")
        });

        diningHall.Items.AddRange(new List<Item>
        {
            new Item("Silver Spoon", "A slightly tarnished silver spoon."),
            new Item("Tablecloth", "A white cloth with a red wine stain in the center."),
            new Item("Candle Holder", "A heavy iron candle holder with wax drippings."),
            new Item("Goblet", "A golden goblet with jewels around the rim."),
            new Item("Locked Chest", "A heavy wooden chest with a brass lock. It seems like it needs a key.", (game) =>
            {
                if (!game.chestOpened && game.inventory.Any(i => i.Name.Equals("Old Key", StringComparison.OrdinalIgnoreCase)))
                {
                    game.chestOpened = true;
                    Console.WriteLine("You unlock the chest with the Old Key. Inside, you find an Old Scroll!");
                    game.currentRoom.Items.Add(new Item("Old Scroll", "An ancient scroll detailing the history of the mansion's reclusive former owner."));
                }
                else if (game.chestOpened)
                {
                    Console.WriteLine("The chest is already open.");
                }
                else
                {
                    Console.WriteLine("The chest is locked. You need a key.");
                }
            })
        });

        foyer.Exits["north"] = library;
        library.Exits["south"] = foyer;
        foyer.Exits["west"] = diningHall;
        diningHall.Exits["east"] = foyer;

        rooms["Foyer"] = foyer;
        rooms["Library"] = library;
        rooms["Dining Hall"] = diningHall;

        currentRoom = foyer;
    }

    public void Start()
    {
        Console.Clear();
        Console.WriteLine("Welcome to the adventure game!");
        currentRoom.Describe();
        while (true)
        {
            Console.Write("\n> ");
            string input = Console.ReadLine().Trim().ToLower();
            Console.Clear();
            if (!ProcessCommand(input)) break;
        }
    }

    private bool ProcessCommand(string command)
    {
        if (command == "look")
        {
            currentRoom.Describe();
        }
        else if (command.StartsWith("inspect "))
        {
            string itemName = command.Substring(8);
            Item item = currentRoom.Items.Concat(inventory)
                .FirstOrDefault(i => i.Name.Equals(itemName, StringComparison.OrdinalIgnoreCase));
            if (item != null)
            {
                item.Inspect();
            }
            else
            {
                Console.WriteLine("There is no such item here or in your inventory.");
            }
        }
        else if (command.StartsWith("take "))
        {
            string itemName = command.Substring(5);
            TakeItem(itemName);
        }
        else if (command.StartsWith("drop "))
        {
            string itemName = command.Substring(5);
            DropItem(itemName);
        }
        else if (command == "inventory")
        {
            ShowInventory();
        }
        else if (command.StartsWith("use "))
        {
            string itemName = command.Substring(4);
            Item item = currentRoom.Items.Concat(inventory)
                .FirstOrDefault(i => i.Name.Equals(itemName, StringComparison.OrdinalIgnoreCase));
            if (item != null && item.UseAction != null)
            {
                item.Use(this);
            }
            else
            {
                Console.WriteLine("You can't use that.");
            }
        }
        else if (command == "save")
        {
            SaveGame();
        }
        else if (command == "load")
        {
            LoadGame();
        }
        else if (command == "quit")
        {
            Console.Write("Are you sure you want to quit? (yes/no): ");
            string response = Console.ReadLine().Trim().ToLower();
            return response != "yes";
        }
        else if (IsDirection(command))
        {
            Move(command);
        }
        else
        {
            Console.WriteLine("Unknown command.");
        }
        return true;
    }

    private bool IsDirection(string command)
    {
        return new[] { "north", "south", "east", "west", "up", "down" }.Contains(command);
    }

    private void Move(string direction)
    {
        if (currentRoom.Exits.ContainsKey(direction))
        {
            currentRoom = currentRoom.Exits[direction];
            Console.WriteLine($"You move {direction}.");
            currentRoom.Describe();
        }
        else
        {
            Console.WriteLine("You can't go that way.");
        }
    }

    private void TakeItem(string itemName)
    {
        if (inventory.Count >= InventoryLimit)
        {
            Console.WriteLine("Your inventory is full. You can't carry more items.");
            return;
        }

        Item item = currentRoom.Items.FirstOrDefault(i => i.Name.Equals(itemName, StringComparison.OrdinalIgnoreCase));
        if (item != null)
        {
            currentRoom.Items.Remove(item);
            inventory.Add(item);
            Console.WriteLine($"You take the {item.Name}.");
        }
        else
        {
            Console.WriteLine("There is no such item here.");
        }
    }

    private void DropItem(string itemName)
    {
        Item item = inventory.FirstOrDefault(i => i.Name.Equals(itemName, StringComparison.OrdinalIgnoreCase));
        if (item != null)
        {
            inventory.Remove(item);
            currentRoom.Items.Add(item);
            Console.WriteLine($"You drop the {item.Name}.");
        }
        else
        {
            Console.WriteLine("You don't have that item.");
        }
    }

    private void ShowInventory()
    {
        if (inventory.Count == 0)
        {
            Console.WriteLine("You are not carrying anything.");
        }
        else
        {
            Console.WriteLine("You are carrying:");
            foreach (var item in inventory)
            {
                Console.WriteLine($"- {item.Name}");
            }
        }
    }

    public void SaveGame()
    {
        var state = new GameState
        {
            CurrentRoomName = currentRoom.Name,
            InventoryItemNames = inventory.Select(i => i.Name).ToList(),
            RoomInventories = rooms.ToDictionary(
                r => r.Key,
                r => r.Value.Items.Select(i => i.Name).ToList()
            ),
            LeverPulled = leverPulled,
            ChestOpened = chestOpened
        };

        string json = JsonSerializer.Serialize(state, new JsonSerializerOptions { WriteIndented = true });
        File.WriteAllText(SaveFileName, json);
        Console.WriteLine("Game saved successfully.");
    }

    public void LoadGame()
    {
        if (!File.Exists(SaveFileName))
        {
            Console.WriteLine("No save file found.");
            return;
        }

        string json = File.ReadAllText(SaveFileName);
        var state = JsonSerializer.Deserialize<GameState>(json);

        if (state == null) return;

        leverPulled = state.LeverPulled;
        chestOpened = state.ChestOpened;
        currentRoom = rooms[state.CurrentRoomName];
        inventory = state.InventoryItemNames.Select(FindItemByName).Where(i => i != null).ToList();

        foreach (var roomEntry in state.RoomInventories)
        {
            if (rooms.TryGetValue(roomEntry.Key, out Room room))
            {
                room.Items.Clear();
                foreach (var itemName in roomEntry.Value)
                {
                    var item = FindItemByName(itemName);
                    if (item != null) room.Items.Add(item);
                }
            }
        }

        if (leverPulled && !rooms["Library"].Exits.ContainsKey("east"))
        {
            rooms["Library"].Exits["east"] = rooms["Secret Room"];
        }

        Console.WriteLine("Game loaded successfully.");
        currentRoom.Describe();
    }

    private Item FindItemByName(string name)
    {
        foreach (var room in rooms.Values)
        {
            foreach (var item in room.Items)
            {
                if (item.Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                    return item;
            }
        }

        if (name == "Old Key") return new Item("Old Key", "A small brass key with intricate carvings.");
        if (name == "Old Scroll") return new Item("Old Scroll", "An ancient scroll detailing the history of the mansion's reclusive former owner.");
        if (name == "Lever") return new Item("Lever", "A small lever hidden behind the globe. It looks like it can be pulled.", (game) =>
        {
            if (!game.leverPulled)
            {
                game.leverPulled = true;
                game.rooms["Library"].Exits["east"] = game.rooms["Secret Room"];
                Console.WriteLine("You pull the lever. You hear a grinding sound as a hidden door opens to the east!");
            }
            else
            {
                Console.WriteLine("The lever has already been pulled.");
            }
        });

        if (name == "Locked Chest") return new Item("Locked Chest", "A heavy wooden chest with a brass lock. It seems like it needs a key.", (game) =>
        {
            if (!game.chestOpened && game.inventory.Any(i => i.Name.Equals("Old Key", StringComparison.OrdinalIgnoreCase)))
            {
                game.chestOpened = true;
                Console.WriteLine("You unlock the chest with the Old Key. Inside, you find an Old Scroll!");
                game.currentRoom.Items.Add(new Item("Old Scroll", "An ancient scroll detailing the history of the mansion's reclusive former owner."));
            }
            else if (game.chestOpened)
            {
                Console.WriteLine("The chest is already open.");
            }
            else
            {
                Console.WriteLine("The chest is locked. You need a key.");
            }
        });

        return new Item(name, "An item with no detailed description.");
    }
}
