using System.Text;
using ClobFts.Core;

namespace ClobFts.App
{
    class Program
    {
        private const string ConnectionString = "Data Source=WINSERVER;Initial Catalog=project;Integrated Security=True;Persist Security Info=False;Pooling=False;Connect Timeout=60;Encrypt=False;TrustServerCertificate=False";
        private static IClobRepository _repository = new SqlClobRepository(ConnectionString);

        static void Main(string[] args)
        {
            Console.WriteLine("CLOB z FTS");
            Console.WriteLine("-----------------------------------------------");

            bool keepRunning = true;
            while (keepRunning)
            {
                Console.WriteLine("\nWybierz opcję:");
                Console.WriteLine("1. Dodaj dokument");
                Console.WriteLine("2. Usuń dokument");
                Console.WriteLine("3. Wyszukaj w dokumentach (po treści)");
                Console.WriteLine("4. Wyszukaj dokumenty (po nazwie)");
                Console.WriteLine("5. Wyjdź");
                Console.Write("Twój wybór: ");

                string choice = Console.ReadLine() ?? "";

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
                            SearchDocumentsByContentUI();
                            break;
                        case "4":
                            SearchDocumentsByNameUI();
                            break;
                        case "5":
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
                    Console.ResetColor();
                }
            }
        }

        private static void AddDocumentUI()
        {
            Console.Write("Podaj nazwę dokumentu: ");
            string? name = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(name))
            {
                Console.WriteLine("Nazwa dokumentu nie może być pusta.");
                return;
            }

            Console.Write("Podaj treść dokumentu (wiele linii, zakończ pisząc 'EOF' na nowej linii):\n");
            string? line;
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
            Console.WriteLine("Dostępne dokumenty:");
            var documentNames = _repository.GetAllDocumentNames();
            if (documentNames.Any())
            {
                foreach (var name in documentNames)
                {
                    Console.Write("- ");
                    Console.ForegroundColor = ConsoleColor.Blue;
                    Console.WriteLine($"{name}");
                    Console.ResetColor();
                }
            }
            else
            {
                Console.WriteLine("Brak dokumentów w bazie.");
                return;
            }

            Console.Write("\nPodaj nazwę dokumentu do usunięcia: ");
            string? nameToDelete = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(nameToDelete))
            {
                Console.WriteLine("Nazwa dokumentu nie może być pusta.");
                return;
            }
            _repository.DeleteDocument(nameToDelete);
            Console.WriteLine($"Dokument '{nameToDelete}' usunięty pomyślnie.");
        }

        private static void DisplayDocuments(List<Tuple<string, string>>? docs)
        {
            if (docs == null)
            {
                return;
            }
            foreach (var doc in docs)
            {

                Console.WriteLine("---");

                Console.WriteLine("Nazwa: ");
                Console.ForegroundColor = ConsoleColor.Blue;
                Console.WriteLine($"{doc.Item1}");
                Console.ResetColor();

                Console.WriteLine("Treść: ");
                Console.ForegroundColor = ConsoleColor.Green;
                Console.WriteLine($"{doc.Item2}");
                Console.ResetColor();

                Console.WriteLine("---");
            }
        }

        private static void SearchDocumentsByContentUI()
        {
            Console.Write("Podaj frazę do wyszukania w treści: ");
            string? query = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(query))
            {
                Console.WriteLine("Fraza do wyszukania nie może być pusta.");
                return;
            }
            var results = _repository.SearchDocuments(query);

            if (results.Any())
            {
                Console.WriteLine("Znalezione dokumenty:");
                DisplayDocuments(results);
            }
            else
            {
                Console.WriteLine("Nie znaleziono dokumentów pasujących do zapytania.");
            }
        }

        private static void SearchDocumentsByNameUI()
        {
            Console.Write("Podaj zapytanie FTS dla nazwy dokumentu (np. 'termin1 OR \"fraza druga\"'): ");
            string? nameQuery = Console.ReadLine();
            if (string.IsNullOrWhiteSpace(nameQuery))
            {
                Console.WriteLine("Zapytanie FTS dla nazwy nie może być puste.");
                return;
            }
            var results = _repository.SearchDocumentsByName(nameQuery);

            if (results.Any())
            {
                Console.WriteLine("Znalezione dokumenty (pasujące nazwą):");
                DisplayDocuments(results);
            }
            else
            {
                Console.WriteLine("Nie znaleziono dokumentów o pasującej nazwie.");
            }
        }
    }
}
