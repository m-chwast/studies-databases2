using System;
using System.Collections.Generic; // For List<T>
using System.Linq; // For .Any()
using System.Text; // For StringBuilder
using ClobFts.Core; // Namespace for our library

namespace ClobFts.App
{
    class Program
    {
        // IMPORTANT: Replace with your actual connection string
        // Example for local SQL Express with Windows Authentication:
        // "Server=localhost\\SQLEXPRESS;Database=ClobFtsDB;Integrated Security=True;TrustServerCertificate=True;"
        // Example with SQL Server Authentication:
        // "Server=your_server_address;Database=ClobFtsDB;User ID=your_user;Password=your_password;TrustServerCertificate=True;"
        private const string ConnectionString = "Server=your_server_name;Database=your_database_name;User ID=your_user_id;Password=your_password;";
        private static IClobRepository _repository;

        static void Main(string[] args)
        {
            // Check if the connection string is a placeholder
            if (ConnectionString.Contains("your_server_name") || 
                ConnectionString.Contains("your_database_name") || 
                (ConnectionString.Contains("your_user_id") && !ConnectionString.ToLower().Contains("integrated security=true")) ||
                (ConnectionString.Contains("your_password") && !ConnectionString.ToLower().Contains("integrated security=true")))
            {
                Console.ForegroundColor = ConsoleColor.Red;
                Console.WriteLine("!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
                Console.WriteLine("!!! PAMIĘTAJ: Zaktualizuj ConnectionString w Program.cs              !!!");
                Console.WriteLine("!!! przed uruchomieniem aplikacji.                                    !!!");
                Console.WriteLine("!!! Przykłady prawidłowych connection stringów są w komentarzu        !!!");
                Console.WriteLine("!!! w kodzie (Program.cs).                                            !!!");
                Console.WriteLine("!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!");
                Console.ResetColor();
                Console.WriteLine("\nNaciśnij dowolny klawisz, aby zakończyć...");
                Console.ReadKey();
                return;
            }

            _repository = new SqlClobRepository(ConnectionString);

            Console.WriteLine("Aplikacja do zarządzania dokumentami CLOB z FTS");
            Console.WriteLine("-----------------------------------------------");

            bool keepRunning = true;
            while (keepRunning)
            {
                Console.WriteLine("\nWybierz opcję:");
                Console.WriteLine("1. Dodaj dokument");
                Console.WriteLine("2. Usuń dokument");
                Console.WriteLine("3. Wyszukaj w dokumentach");
                Console.WriteLine("4. Wyjdź");
                Console.Write("Twój wybór: ");

                string choice = Console.ReadLine();

                try
                {
                    switch (choice)
                    {
                        case "1":
                            AddDocumentUI();
                            break;
                        case "2":
                            DeleteDocumentUI();
                            break;
                        case "3":
                            SearchDocumentsUI();
                            break;
                        case "4":
                            keepRunning = false;
                            break;
                        default:
                            Console.WriteLine("Nieprawidłowy wybór. Spróbuj ponownie.");
                            break;
                    }
                }
                catch (Exception ex)
                {
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Wystąpił błąd: {ex.Message}");
                    // Dla celów deweloperskich można odkomentować poniższe:
                    // Console.WriteLine($"Szczegóły: {ex.ToString()}");
                    Console.ResetColor();
                }
            }
        }

        private static void AddDocumentUI()
        {
            Console.Write("Podaj nazwę dokumentu: ");
            string name = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(name))
            {
                Console.WriteLine("Nazwa dokumentu nie może być pusta.");
                return;
            }

            Console.Write("Podaj treść dokumentu (wiele linii, zakończ pisząc 'EOF' na nowej linii):\n");
            string line;
            var contentBuilder = new StringBuilder();
            while ((line = Console.ReadLine()) != null && line.Trim().ToUpper() != "EOF")
            {
                contentBuilder.AppendLine(line);
            }
            string content = contentBuilder.ToString();

            _repository.AddDocument(name, content);
            Console.WriteLine("Dokument dodany pomyślnie.");
        }

        private static void DeleteDocumentUI()
        {
            Console.Write("Podaj nazwę dokumentu do usunięcia: ");
            string name = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(name))
            {
                Console.WriteLine("Nazwa dokumentu nie może być pusta.");
                return;
            }
            _repository.DeleteDocument(name);
            Console.WriteLine($"Dokument '{name}' usunięty pomyślnie.");
        }

        private static void SearchDocumentsUI()
        {
            Console.Write("Podaj frazę do wyszukania: ");
            string query = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(query))
            {
                Console.WriteLine("Fraza do wyszukania nie może być pusta.");
                return;
            }
            var results = _repository.SearchDocuments(query);

            if (results.Any())
            {
                Console.WriteLine("Znalezione dokumenty:");
                foreach (var docName in results)
                {
                    Console.WriteLine($"- {docName}");
                }
            }
            else
            {
                Console.WriteLine("Nie znaleziono dokumentów pasujących do zapytania.");
            }
        }
    }
}
