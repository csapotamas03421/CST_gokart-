using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace CST_gokart
{
    class Versenyzo
    {
        public string Vezeteknev { get; set; }
        public string Keresztnev { get; set; }
        public DateTime SzuletesiIdo { get; set; }
        public bool Elmult_e_18 { get; set; }
        public string Versenyzo_azonosito { get; set; }
        public string Email { get; set; }

        public Versenyzo(string vezeteknev, string keresztnev, DateTime szuletesiIdo)
        {
            Vezeteknev = vezeteknev;
            Keresztnev = keresztnev;
            SzuletesiIdo = szuletesiIdo;
            Elmult_e_18 = folott18(szuletesiIdo);
            Versenyzo_azonosito = GeneratalId();
            Email = GeneratalEmail();
        }

        bool folott18(DateTime birth)
        {
            DateTime today = DateTime.Today;
            int age = today.Year - birth.Year;
            if (birth > today.AddYears(-age)) age--;
            return age >= 18;
        }

        string RemoveDiacritics(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            string normalized = text.Normalize(NormalizationForm.FormD);
            StringBuilder sb = new StringBuilder();
            foreach (char c in normalized)
            {
                var uc = CharUnicodeInfo.GetUnicodeCategory(c);
                if (uc != UnicodeCategory.NonSpacingMark) sb.Append(c);
            }
            return sb.ToString().Normalize(NormalizationForm.FormC);
        }

        string GeneratalId()
        {
            string full = RemoveDiacritics(Vezeteknev + Keresztnev).Replace(" ", "");
            return $"GO-{full}-{SzuletesiIdo:yyyyMMdd}";
        }

        string GeneratalEmail()
        {
            string v = RemoveDiacritics(Vezeteknev).ToLower().Replace(" ", "");
            string k = RemoveDiacritics(Keresztnev).ToLower().Replace(" ", "");
            return $"{v}.{k}@gmail.com";
        }
    }

    class Foglalas
    {
        public DateTime Kezdete { get; set; }
        public DateTime Vege { get; set; }
        public List<string> VersenyzoAzonositok { get; set; }

        public Foglalas(DateTime kezdete, DateTime vege)
        {
            Kezdete = kezdete;
            Vege = vege;
            VersenyzoAzonositok = new List<string>();
        }
    }

    class Foglalaskeszito
    {
        public List<Foglalas> Foglalasok { get; set; } = new List<Foglalas>();
        public int NyitasOra = 8;
        public int ZarasOra = 20;
        public int MinResztvevo = 8;
        public int MaxResztvevo = 20;

        public bool Utkkozik(DateTime kezd, DateTime vege)
        {
            foreach (var f in Foglalasok)
            {
                if (f.Kezdete.Date == kezd.Date && ((kezd >= f.Kezdete && kezd < f.Vege) || (vege > f.Kezdete && vege <= f.Vege)))
                    return true;
            }
            return false;
        }

        public void AddFoglalas(DateTime kezd, int idotartam, Dictionary<string, Versenyzo> versenyzok, Random rnd)
        {
            // Időtartam ellenőrzése
            if (idotartam < 1 || idotartam > 2)
            {
                Console.WriteLine("Csak 1 vagy 2 órára lehet foglalni!");
                return;
            }

            DateTime vege = kezd.AddHours(idotartam);

            // Múltbeli dátum ellenőrzése
            if (kezd < DateTime.Now)
            {
                Console.WriteLine("Nem lehet múltbeli dátumra foglalni!");
                return;
            }

            // Mai napon a jelenlegi időnél korábbi ellenőrzése
            if (kezd.Date == DateTime.Today && kezd.TimeOfDay < DateTime.Now.TimeOfDay)
            {
                Console.WriteLine("Nem lehet a jelenlegi időpontnál korábbra foglalni!");
                return;
            }

            if (Utkkozik(kezd, vege))
            {
                Console.WriteLine("Az időpont ütközik egy másik foglalással!");
                return;
            }

            if (kezd.Hour < NyitasOra || vege.Hour > ZarasOra || (kezd.Hour == NyitasOra && kezd.Minute > 0))
            {
                Console.WriteLine("Az időpont kívül esik a nyitvatartási időn (8:00-20:00)!");
                return;
            }

            Foglalas uj = new Foglalas(kezd, vege);
            int resztvevok = rnd.Next(MinResztvevo, MaxResztvevo + 1);
            List<string> osszesId = new List<string>(versenyzok.Keys);

            for (int i = 0; i < resztvevok; i++)
            {
                int idx = rnd.Next(osszesId.Count);
                uj.VersenyzoAzonositok.Add(osszesId[idx]);
                osszesId.RemoveAt(idx);
            }

            Foglalasok.Add(uj);
            Console.WriteLine($"Sikeres foglalás: {kezd:yyyy.MM.dd HH}:00 - {vege:HH}:00 ({resztvevok} versenyző)");
        }

        public void ShowFoglalasok(Dictionary<string, Versenyzo> versenyzok)
        {
            if (Foglalasok.Count == 0)
            {
                Console.WriteLine("Még nincs foglalás!");
                return;
            }

            Console.WriteLine("\nFoglalások listája:");
            for (int i = 0; i < Foglalasok.Count; i++)
            {
                var f = Foglalasok[i];
                Console.WriteLine($"{i + 1}. {f.Kezdete:yyyy.MM.dd HH}:00 - {f.Vege:HH}:00 ({f.VersenyzoAzonositok.Count} fő)");
            }

            Console.Write("\nVálassz foglalást (sorszám): ");
            if (int.TryParse(Console.ReadLine(), out int index) && index > 0 && index <= Foglalasok.Count)
            {
                var fogl = Foglalasok[index - 1];
                ShowFoglalasReszletek(fogl, versenyzok);
            }
            else
            {
                Console.WriteLine("Érvénytelen sorszám!");
            }
        }

        void ShowFoglalasReszletek(Foglalas fogl, Dictionary<string, Versenyzo> versenyzok)
        {
            Console.WriteLine($"\nFoglalás: {fogl.Kezdete:yyyy.MM.dd HH}:00 - {fogl.Vege:HH}:00");
            Console.WriteLine($"Résztvevők száma: {fogl.VersenyzoAzonositok.Count}\n");

            foreach (string id in fogl.VersenyzoAzonositok)
            {
                var v = versenyzok[id];
                Console.WriteLine($"- {v.Versenyzo_azonosito}");
            }

            Console.WriteLine("\n1 - Időpont áthelyezése");
            Console.WriteLine("2 - Vissza");
            Console.Write("Választás: ");
            string val = Console.ReadLine();

            switch (val)
            {
                case "1":
                    Idopontmozgatas(fogl, versenyzok);
                    break;
                default:
                    return;
            }
        }

        void Idopontmozgatas(Foglalas foglalas, Dictionary<string, Versenyzo> versenyzok)
        {
            Console.Write("Új dátum (yyyy.MM.dd): ");
            if (!DateTime.TryParseExact(Console.ReadLine(), "yyyy.MM.dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime ujNap))
            {
                Console.WriteLine("Hibás dátumformátum!");
                return;
            }

            // Dátum ellenőrzése - nem lehet múltbeli
            if (ujNap.Date < DateTime.Today)
            {
                Console.WriteLine("Nem lehet múltbeli dátumra foglalni!");
                return;
            }

            Console.Write("Új kezdőóra (8:00 – 19:00): ");
            if (!int.TryParse(Console.ReadLine(), out int ujOra))
            {
                Console.WriteLine("Hibás óra!");
                return;
            }

            // Óra ellenőrzése
            if (ujOra < 8 || ujOra > 19)
            {
                Console.WriteLine("Az óra 8 és 19 között lehet!");
                return;
            }

            // Mai nap ellenőrzése - nem lehet korábbi időpont
            if (ujNap.Date == DateTime.Today && ujOra < DateTime.Now.Hour)
            {
                Console.WriteLine("Nem lehet a jelenlegi időpontnál korábbra foglalni!");
                return;
            }

            // Az időtartam meghatározása a régi foglalás alapján
            int idotartam = (int)(foglalas.Vege - foglalas.Kezdete).TotalHours;

            DateTime ujKezd = new DateTime(ujNap.Year, ujNap.Month, ujNap.Day, ujOra, 0, 0);
            DateTime ujVege = ujKezd.AddHours(idotartam);

            // Ellenőrzések az új időpontra
            if (Utkkozik(ujKezd, ujVege))
            {
                Console.WriteLine("Az új időpont ütközik egy másik foglalással!");
                return;
            }

            if (ujKezd.Hour < NyitasOra || ujVege.Hour > ZarasOra || (ujKezd.Hour == NyitasOra && ujKezd.Minute > 0))
            {
                Console.WriteLine("Az új időpont kívül esik a nyitvatartási időn (8:00-20:00)!");
                return;
            }

            // Az időpont módosítása
            foglalas.Kezdete = ujKezd;
            foglalas.Vege = ujVege;

            Console.WriteLine($"Időpont sikeresen áthelyezve: {ujKezd:yyyy.MM.dd HH}:00 - {ujVege:HH}:00");
        }

        public void ShowMonthTimeline()
        {
            DateTime today = DateTime.Now.Date;
            DateTime endOfMonth = new DateTime(today.Year, today.Month, DateTime.DaysInMonth(today.Year, today.Month));

            Console.WriteLine("\nIdőtáblázat (zöld = szabad, piros = foglalt):");
            for (DateTime d = today; d <= endOfMonth; d = d.AddDays(1))
            {
                Console.ForegroundColor = ConsoleColor.Cyan;
                Console.WriteLine($"\n{d:yyyy.MM.dd dddd}");
                Console.ResetColor();

                for (int ora = NyitasOra; ora < ZarasOra; ora++)
                {
                    bool foglalt = false;
                    foreach (var f in Foglalasok)
                    {
                        if (f.Kezdete.Date == d && ora >= f.Kezdete.Hour && ora < f.Vege.Hour)
                        {
                            foglalt = true;
                            break;
                        }
                    }

                    Console.ForegroundColor = foglalt ? ConsoleColor.Red : ConsoleColor.Green;
                    Console.Write($"[{ora:00}:00]");
                    Console.ResetColor();
                    Console.Write(" ");
                }
                Console.WriteLine();
            }
        }
    }

    internal class Program
    {
        static void Main()
        {
            Console.OutputEncoding = Encoding.UTF8;
            Console.WriteLine("CST Gokart időpontfoglaló\n");

            Random rnd = new Random();

            Console.WriteLine("GokartTrack | 1011, Budapest I. kerület Kossuth Lajos út 12 | GokartTrack.hu | gokartrack@gmail.com");
            Console.WriteLine();

            #region Versenyzők adatai
            string beolvas_v = File.ReadAllText("vezeteknevek.txt");
            List<string> vezeteknevek = new List<string>();
            foreach (var item in beolvas_v.Split(','))
                vezeteknevek.Add(item.Trim('\'', ' '));

            string beolvas_k = File.ReadAllText("keresztnevek.txt");
            List<string> keresztnevek = new List<string>();
            foreach (var item in beolvas_k.Split(','))
                keresztnevek.Add(item.Trim('\'', ' '));

            Dictionary<string, Versenyzo> versenyzok = new Dictionary<string, Versenyzo>();

            for (int i = 1; i <= 150; i++)
            {
                int randomIndex_v = rnd.Next(vezeteknevek.Count);
                int randomIndex_k = rnd.Next(keresztnevek.Count);
                string vezeteknev = vezeteknevek[randomIndex_v];
                string keresztnev = keresztnevek[randomIndex_k];
                DateTime szuletesiIdo = new DateTime(rnd.Next(1960, 2025), rnd.Next(1, 13), rnd.Next(1, 29));

                Versenyzo uj_versenyzo = new Versenyzo(vezeteknev, keresztnev, szuletesiIdo);
                versenyzok.Add(uj_versenyzo.Versenyzo_azonosito, uj_versenyzo);
            }

            Console.WriteLine($"Versenyzők generálva: {versenyzok.Count} db\n");
            #endregion

            Foglalaskeszito fm = new Foglalaskeszito();
            fm.ShowMonthTimeline();

            bool kilep = false;
            while (!kilep)
            {
                Console.WriteLine("\n1 - Új foglalás");
                Console.WriteLine("2 - Foglalások megtekintése / szerkesztése");
                Console.WriteLine("3 - Időtáblázat frissítése");
                Console.WriteLine("4 - Kilépés");
                Console.Write("Választás: ");

                string valasz = Console.ReadLine();
                switch (valasz)
                {
                    case "1":
                        Console.Write("Dátum (yyyy.MM.dd): ");
                        if (!DateTime.TryParseExact(Console.ReadLine(), "yyyy.MM.dd", CultureInfo.InvariantCulture, DateTimeStyles.None, out DateTime nap))
                        {
                            Console.WriteLine("Hibás dátumformátum!");
                            break;
                        }

                        // Dátum ellenőrzése - nem lehet múltbeli
                        if (nap.Date < DateTime.Today)
                        {
                            Console.WriteLine("Nem lehet múltbeli dátumra foglalni!");
                            break;
                        }

                        Console.Write("Kezdőóra (8–19): ");
                        if (!int.TryParse(Console.ReadLine(), out int ora))
                        {
                            Console.WriteLine("Hibás óra!");
                            break;
                        }

                        // Óra ellenőrzése
                        if (ora < 8 || ora > 19)
                        {
                            Console.WriteLine("Az óra 8 és 19 között lehet!");
                            break;
                        }

                        // Mai nap ellenőrzése - nem lehet korábbi időpont
                        if (nap.Date == DateTime.Today && ora < DateTime.Now.Hour)
                        {
                            Console.WriteLine("Nem lehet a jelenlegi időpontnál korábbra foglalni!");
                            break;
                        }

                        Console.Write("Időtartam (1 vagy 2 óra): ");
                        if (!int.TryParse(Console.ReadLine(), out int idotartam))
                        {
                            Console.WriteLine("Hibás időtartam!");
                            break;
                        }

                        // Időtartam ellenőrzése
                        if (idotartam != 1 && idotartam != 2)
                        {
                            Console.WriteLine("Csak 1 vagy 2 órára lehet foglalni!");
                            break;
                        }

                        DateTime kezd = new DateTime(nap.Year, nap.Month, nap.Day, ora, 0, 0);
                        fm.AddFoglalas(kezd, idotartam, versenyzok, rnd);
                        break;

                    case "2":
                        fm.ShowFoglalasok(versenyzok);
                        break;

                    case "3":
                        fm.ShowMonthTimeline();
                        break;

                    case "4":
                        kilep = true;
                        break;

                    default:
                        Console.WriteLine("Ismeretlen opció!");
                        break;
                }
            }

            Console.WriteLine("\nKilépéshez ENTER.");
            Console.ReadLine();
        }
    }
}
