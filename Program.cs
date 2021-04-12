using System;
using System.Collections.Generic;
using System.Security.Cryptography;
using System.Text;
using System.Globalization;
using System.IO;
using System.Linq;
using Newtonsoft.Json;

namespace VozovyPark
{
    class Program
    {
        public delegate User UDel();
        public delegate int IDel();
        static User curruser;
        static Dictionary<string, User> users = new Dictionary<string, User>();
        static Dictionary<string, Car> cars = new Dictionary<string, Car>();
        static Dictionary<string, List<Reservation>> reservations = new Dictionary<string, List<Reservation>>(); //Key = userId

        public class Reservation
        {
            public DateTime startDate;
            public DateTime endDate;
            public string userId;
            public string carId;
            public Reservation(DateTime startdate, DateTime enddate, string userid, string carid)
            {
                this.startDate = startdate;
                this.endDate = enddate;
                this.userId = userid;
                this.carId = carid;
            }
        }
        public class User
        {
            public string uname;
            public string fname;
            public string lname;
            public string passwd;
            public DateTime lastlogin;
            public bool admin { get; }
            public User(string uname, string fname, string lname, string passwd, bool admin, bool plaintextpassword = false)
            {

                this.fname = fname;
                this.lname = lname;
                if (plaintextpassword)
                {
                    SHA256 hash = SHA256.Create();
                    byte[] encrypted = hash.ComputeHash(Encoding.ASCII.GetBytes(passwd));
                    this.passwd = BitConverter.ToString(encrypted);
                }
                else
                {
                    this.passwd = passwd;
                }
                this.uname = uname;
                this.lastlogin = DateTime.Now;
                this.admin = admin;
            }
        }
        class Car
        {
            public string id;
            public string brand;
            public string model;
            public string type;
            public float consumption; //Per 100km (l)
            public double cost; 
            public Car(string carid, string brand, string model, string type, float consumption, double cost)
            {
                this.id = carid;
                this.brand = brand;
                this.model = model;
                this.type = type;
                this.consumption = consumption;
                this.cost = cost;
            }
        }
        public static User Login()
        {
            Console.WriteLine("Insert username: ");
            string uname = Console.ReadLine();
            Console.WriteLine("\nInsert password: ");
            string passwd = Console.ReadLine();
            SHA256 hash = SHA256.Create();
            byte[] encrypted = hash.ComputeHash(Encoding.ASCII.GetBytes(passwd));
            passwd = BitConverter.ToString(encrypted);
            int status = Auth(uname, passwd);
            switch (status)
            {
                case 0:
                    return users[uname];
                    break;
                case 1:
                    Console.WriteLine("Wrong username");
                    Console.ReadKey();
                    break;
                case 2:
                    Console.WriteLine("Wrong password");
                    Console.ReadKey();
                    break;
            }
            return null;
        }
    
        public static int Auth(string name, string passwd)
        {
            if (users.ContainsKey(name))
            {
                if (users[name].passwd == passwd)
                {
                    return (0);
                }
                else
                {
                    return (2);
                }
            }
            else
            {
                return (1);
            }
        }
        static int Logout()
        {
            curruser = Start(0);
            Console.Clear();
            return 0;
        }
        static int PasswdChange()
        {
            Console.WriteLine("Insert new password: ");
            string npass = Console.ReadLine();
            users[curruser.uname].passwd = npass;
            return (0);
        }
        public static User Register()
        {
            while (true)
            {
                User user;
                registerStart:
                Console.WriteLine("Insert first name: ");
                string fname = Console.ReadLine();
                Console.WriteLine("Insert last name: ");
                string lname = Console.ReadLine();
                Console.WriteLine("Insert password: ");
                string passwd = Console.ReadLine();
                Console.WriteLine("Insert username: ");
                string uname = Console.ReadLine();
                switch (Auth(uname, passwd))
                {
                    case 1:
                        user = new User(uname, fname, lname, passwd, false, true);
                        users.Add(uname, user);
                        Console.WriteLine("Uzivatel vytvoren, prihlaste se za pomoci uzivatelskeho jmena a hesla");
                        Console.ReadKey();
                        return (user);
                        break;
                    default:
                        Console.WriteLine("Uzivatelske jmeno pouzito, prosim vyberte jine");
                        string[] menuFields = { "Zmenit uzivatelske jmeno", "Zmenit heslo", "Zmenit vse", "Zrusit" };
                        switch (ShowMenu(menuFields, "", "", 0))
                        {
                            case 0:
                                Console.WriteLine("Insert username: ");
                                uname = Console.ReadLine();
                                break;
                            case 1:
                                Console.WriteLine("Insert password: ");
                                passwd = Console.ReadLine();
                                break;
                            case 2:
                                goto registerStart;
                                break;
                            case 3:
                                Start(0);
                                return null; //Aborted
                        }
                        break;
                }
            }
        }
        static int ShowMenu(string[] menu, string header, string footer, int highlight)
        {
            Console.Clear();
            int highlighted = highlight;
            Console.WriteLine(header);
            for (int i = 0; i < menu.Length; i++)
                {
                    if (i == highlighted)
                    {
                        Console.BackgroundColor = ConsoleColor.White;
                        Console.ForegroundColor = ConsoleColor.Black;
                    }
                    Console.WriteLine(menu[i]);
                    Console.ResetColor();
                }
            Console.WriteLine(footer);
            while (true)
            {
                ConsoleKeyInfo t = Console.ReadKey();
                switch (t.Key)
                {
                    case ConsoleKey.UpArrow:
                        if (highlighted > 0)
                        {
                            highlighted--;
                        }
                        else
                        {
                            highlighted = menu.Length - 1;
                        }
                        break;
                    case ConsoleKey.DownArrow:
                        if (highlighted < menu.Length-1)
                        {
                            highlighted++;
                        }
                        else
                        {
                            highlighted = 0;
                        }
                        break;
                    case ConsoleKey.Enter:
                    case ConsoleKey.Spacebar:
                        return (highlighted);
                        break;
                }
                return(ShowMenu(menu, header, footer, highlighted));
            }
        }
        static User Start(int highl)
        {
            int highlighted = highl;
            string[] options = { "Prihlasit", "Zaregistrovat" };
            UDel[] functions = { Login, Register };
            int choice;
            User user;
            do
            {
                choice = ShowMenu(options, "---Vozovy park---\n\n\n", "", 0);
                user = functions[choice]();
            } while (!(user != null && choice == 0));
            return user;
        }
        static string[][] carDisplay()
        {
            List<string> carnames = new List<string>();
            List<Car> desc = new List<Car>();
            string[][] cardata = new string[cars.Count][];
            int i = 0;
            foreach (KeyValuePair<string, Car> car in cars)
            {
                cardata[i] = new string[7];
                cardata[i][0] = car.Key;
                cardata[i][1] = car.Value.brand;
                cardata[i][2] = car.Value.consumption.ToString();
                cardata[i][3] = car.Value.cost.ToString();
                cardata[i][4] = car.Value.id;
                cardata[i][5] = car.Value.model;
                cardata[i][6] = car.Value.type;
                i++;
            }
            
            return cardata;
        }
        static int CurrRes()
        {
            Console.Clear();
            bool hasReservations = false;
            if (!reservations.ContainsKey(curruser.uname))
            {
                reservations.Add(curruser.uname, new List<Reservation>());
            }
            for (int i = 0; i < reservations[curruser.uname].Count; i++)
            {
                hasReservations = true;
                string cRes;
                cRes = reservations[curruser.uname][i].carId.ToString() + " = " + reservations[curruser.uname][i].startDate.ToString() + " - " + reservations[curruser.uname][i].endDate.ToString();
                Console.WriteLine(cRes);
            }
            if (!hasReservations)
            {
                Console.WriteLine("Žádné rezervace");
            }
            Console.ReadKey();
            return (0);
        }
        static int NewRes()
        {
            string startdates;
            DateTime startDate;
            bool correctFormat;
            while (true)
            {
                Console.WriteLine("Zacatecni datum: ");
                startdates = Console.ReadLine();
                correctFormat = DateTime.TryParse(startdates, out startDate);
                if (!correctFormat)
                {
                    Console.WriteLine("Spatny format, zkuste znovu");
                }
                else if (startDate < DateTime.Now)
                {
                    Console.WriteLine("Datum je v minulosti");
                }
                else
                {
                    break;
                }
            }
            DateTime endDate;
            while (true)
            {
                Console.WriteLine("Koncove datum: ");
                string enddates = Console.ReadLine();
                correctFormat = DateTime.TryParse(enddates, out endDate);
                if (!correctFormat)
                {
                    Console.WriteLine("Spatny format, zkuste znovu");
                }
                else if (endDate < startDate)
                {
                    Console.WriteLine("Konecne datum je pred zacatecnim");
                }
                else
                {
                    break;
                }
            }
            string carid = selcar();
            if (!reservations.ContainsKey(curruser.uname))
            {
                reservations.Add(curruser.uname, new List<Reservation>());
            }
            reservations[curruser.uname].Add(new Reservation(startDate, endDate, curruser.uname, carid));
            return (0);
        }
        static int CancelRes()
        {
            if (!reservations.ContainsKey(curruser.uname))
            {
                Console.WriteLine("Zadne rezervace");
                Console.ReadKey();
                return (0);
            }
            string[] restring = new string[reservations[curruser.uname].Count];
            int i = 0;
            foreach(var res in reservations[curruser.uname])
            {
                restring[i] = res.carId + "\t - \t" + "Od " + res.startDate + " do " + res.endDate;
                i++;
            }
            int sel = ShowMenu(restring, "Vyberte rezervaci: ", "", 0);
            reservations[curruser.uname].RemoveAt(sel);
            return (0);
        }
        static int AddUser()
        {
            while (true)
            {
                User user;
            registerStart:
                Console.WriteLine("Insert first name: ");
                string fname = Console.ReadLine();
                Console.WriteLine("Insert last name: ");
                string lname = Console.ReadLine();
                Console.WriteLine("Insert password: ");
                string passwd = Console.ReadLine();
                
                Console.WriteLine("Insert username: ");
                string uname = Console.ReadLine();
                string[] role = { "user", "admin" };
                bool admin = ShowMenu(role, "", "", 0)==1;
                switch (Auth(uname, passwd))
                {
                    case 1:
                        user = new User(uname, fname, lname, passwd, admin);
                        users.Add(uname, user);
                        return (0);
                        break;
                    default:
                        Console.WriteLine("Uzivatelske jmeno pouzito, prosim vyberte jine");
                        string[] menuFields = { "Zmenit uzivatelske jmeno", "Zmenit heslo", "Zmenit vse", "Zrusit" };
                        switch (ShowMenu(menuFields, "", "", 0))
                        {
                            case 0:
                                Console.WriteLine("Insert username: ");
                                uname = Console.ReadLine();
                                break;
                            case 1:
                                Console.WriteLine("Insert password: ");
                                passwd = Console.ReadLine();
                                break;
                            case 2:
                                goto registerStart;
                                break;
                            case 3:
                                return 1; //Aborted
                        }
                        break;
                }
            }
        }
        static int RemUser()
        {
            while (true)
            {
                string[] unames = new string[users.Count];
                int i = 0;
                foreach (var us in users)
                {
                    unames[i] = us.Key;
                    i++;
                }
                int sel = ShowMenu(unames, "Vyberte uzivatele: ", "", 0);
                string uname = unames[sel];
                if (users.ContainsKey(uname))
                {
                    users.Remove(uname);
                    break;
                }
                else
                {
                    Console.WriteLine("Not present");
                }
            }
            return 0;
        }
        static int AddCar()
        {
            Console.WriteLine("Insert Car ID: ");
            string carId = Console.ReadLine();
            Console.WriteLine("Insert Car brand: ");
            string brand = Console.ReadLine();
            Console.WriteLine("Insert Car model: ");
            string model = Console.ReadLine();
            Console.WriteLine("Insert Car type: ");
            string type = Console.ReadLine();
            float consumption;
            while (true)
            {
                Console.WriteLine("Insert Car consumption (l/100km): ");
                bool correct = float.TryParse(Console.ReadLine(), out consumption);
                if (correct)
                {
                    break;
                }
                else
                {
                    Console.WriteLine("Wrong format");
                }
            }
            double cost;
            while (true)
            {
                Console.WriteLine("Insert Car cost (kc/den): ");
                bool correct = double.TryParse(Console.ReadLine(), out cost);
                if (correct)
                {
                    break;
                }
                else
                {
                    Console.WriteLine("Wrong format");
                }
            }

            cars.Add(carId, new Car(carId, brand, model, type, consumption, cost));
            return 0;   
        }
        static string selcar()
        {
            string[][] cars = carDisplay();
            string[] carstring = new string[cars.Length];
            for (int i = 0; i < cars.Length; i++)
            {
                carstring[i] = cars[i][1] + " " + cars[i][5] + " - " + cars[i][6] + "\t|\t" + cars[i][3] + "kc/den\t-\t" + cars[i][2] + "l/100km";
            }
            int selected = ShowMenu(carstring, "Auta:", "", 0);
            return cars[selected][0];
        }
        static int AddResAdmin()

        {
            string uname;
            while (true)
            {
                Console.WriteLine("Insert user name:");
                uname = Console.ReadLine();
                int res = Auth(uname, "");
                if (res == 2)
                {
                    break;
                }
                else
                {
                    Console.WriteLine("User doesn't exist");
                }

            }
            string startdates;
            DateTime startDate;
            bool correctFormat;
            while (true)
            {
                Console.WriteLine("Zacatecni datum: ");
                startdates = Console.ReadLine();
                correctFormat = DateTime.TryParse(startdates, out startDate);
                if (!correctFormat)
                {
                    Console.WriteLine("Spatny format, zkuste znovu");
                }
                else
                {
                    break;
                }
            }
            DateTime endDate;
            while (true)
            {
                Console.WriteLine("Koncove datum: ");
                string enddates = Console.ReadLine();
                correctFormat = DateTime.TryParse(enddates, out endDate);
                if (!correctFormat)
                {
                    Console.WriteLine("Spatny format, zkuste znovu");
                }
                else
                {
                    break;
                }
            }
            string carid = selcar();

            if (!reservations.ContainsKey(curruser.uname))
            {
                reservations.Add(uname, new List<Reservation>());
            }
            if (!reservations.ContainsKey(uname))
            {
                reservations.Add(uname, new List<Reservation>());
            }
            reservations[uname].Add(new Reservation(startDate, endDate, uname, carid));
            return 0;
        }
        static int RemCar()
        {
            string carId;
            while (true)
            {
                Console.WriteLine("CarId: ");
                carId = Console.ReadLine();
                if (!cars.ContainsKey(carId))
                {
                    Console.WriteLine("Spatny format, zkuste znovu");
                }
                else
                {
                    break;
                }
            }
            cars.Remove(carId);
            foreach (var userres in reservations)
            {
                for (int i = 0; i < userres.Value.Count; i++)
                {
                    Reservation res = userres.Value[i];
                    if (res.carId == carId)
                    {
                        userres.Value.Remove(res);
                        i--;
                    }
                }
            }
            return 0;
        }
        static int ChangePass() //Change password of a user
        {
            string uname;
            while (true)
            {
                Console.WriteLine("Insert uname: ");
                uname = Console.ReadLine();
                if (users.ContainsKey(uname))
                {
                    break;
                }
                else
                {
                    Console.WriteLine("Not present");
                }
            }
            Console.WriteLine("Change to: ");
            users[uname].passwd = Console.ReadLine();
            return 0;
        }

        /*SAVEFILE FORMAT = 
        [
        {USERS} 
        {CARS}
        {RESERVATIONS}
        ]
        */
        static int Save()
        {
            string savefilepath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            savefilepath = Path.Combine(savefilepath, "MrazekVozovyPark.json");
            Console.WriteLine("Writing to " + savefilepath);
            string userjson = JsonConvert.SerializeObject(users);
            string carjson = JsonConvert.SerializeObject(cars);
            string resjson = JsonConvert.SerializeObject(reservations);
            string res = userjson + "\n" + carjson + "\n" + resjson;
            File.WriteAllText(savefilepath, res);
            return (0);
        }

        static int Load()
        {
            string savefilepath = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);
            savefilepath = Path.Combine(savefilepath, "MrazekVozovyPark.json");
            Console.WriteLine("Reading from " + savefilepath);
            string[] data = File.ReadLines(savefilepath).ToArray();
            users = JsonConvert.DeserializeObject<Dictionary<string, User>>(data[0]);
            int i = 0;
            foreach(var usr in users)
            {
                usr.Value.passwd = JsonConvert.DeserializeObject<Dictionary<string, User>>(data[0])[usr.Key].passwd;
            }
            cars = JsonConvert.DeserializeObject<Dictionary<string, Car>>(data[1]);
            reservations = JsonConvert.DeserializeObject<Dictionary<string, List<Reservation>>>(data[2]);
            return (0);
        }
        static void Main(string[] args)
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;
            users.Add("admin", new User("admin", "Lukas", "Mrazek", "admin", true, true));
            users.Add("pepa", new User("pepa", "Pepa", "Pepis", "pepafrajer420", false, true));
            users.Add("jenda", new User("jenda", "Jenda", "Jendis", "pepasmrdi69", false, true));
            users.Add("test", new User("test", "Tester", "Testis", "test", false, true));
            curruser = Start(0); 
            string[] userMenu = { "Změna hesla", "Přehled aktuálních rezervací", "Zadání rezervace", "Zrušení rezervace", "Odhlášení" };
            string[] adminMenu = { "Založení uživatele", "Zrušení uživatele", "Vložení auta", "Vložení rezervace jménem uživatele", "Zrušení auta", "Vynucení změny hesla", "Odhlášení", "Ulozit", "Nahrat ze souboru" };
            string[] menu = curruser.admin ? adminMenu : userMenu;
            IDel[] userFunctions = {  PasswdChange, CurrRes, NewRes, CancelRes, Logout };
            IDel[] adminFunctions = { AddUser, RemUser, AddCar, AddResAdmin, RemCar, ChangePass, Logout, Save, Load };
            IDel[] functions = curruser.admin ? adminFunctions : userFunctions;
            cars.Add("auto1", new Car("auto1", "Skoda", "Octavia", "Osobni", 0.5f, 500));
            cars.Add("auto2", new Car("auto2", "Opel", "Cruiser", "Nakladni", 2, 100));
            while (true)
            {
                functions[ShowMenu(menu, "Vozovy park - " + curruser.fname, "", 0)]();
                menu = curruser.admin ? adminMenu : userMenu;
                functions = curruser.admin ? adminFunctions : userFunctions;
            }
        }
    }
}
