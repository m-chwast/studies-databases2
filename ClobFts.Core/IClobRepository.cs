using System.Collections.Generic;

namespace ClobFts.Core
{
    public interface IClobRepository
    {
        void AddDocument(string documentName, string content);
        void DeleteDocument(string documentName);
        List<Tuple<string, string>> SearchDocuments(string searchQuery);
        List<Tuple<string, string>> SearchDocumentsByName(string documentNameQuery);
    }
}
