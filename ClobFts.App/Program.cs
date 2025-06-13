using System;
using System.Collections.Generic; // For List<T>
using System.Linq; // For .Any()
using System.Text; // For StringBuilder
using ClobFts.Core; // Namespace for our library

namespace ClobFts.App
{
    class Program
    {
        private const string ConnectionString = "Data Source=WINSERVER;Initial Catalog=testCLR;Integrated Security=True;Persist Security Info=False;Pooling=False;Connect Timeout=60;Encrypt=False;TrustServerCertificate=False";
        private static IClobRepository _repository;

        static void Main(string[] args)
        {
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
            Console.Write("Podaj nazwę dokumentu do usunięcia: ");
            string? name = Console.ReadLine();
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
