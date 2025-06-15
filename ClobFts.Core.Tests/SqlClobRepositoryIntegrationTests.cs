using Microsoft.VisualStudio.TestTools.UnitTesting;
using ClobFts.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace ClobFts.Core.Tests
{
    [TestClass]
    public class SqlClobRepositoryIntegrationTests
    {
        private const string TestConnectionString = "Data Source=WINSERVER;Initial Catalog=testCLR;Integrated Security=True;Persist Security Info=False;Pooling=False;Connect Timeout=60;Encrypt=False;TrustServerCertificate=False";
        private SqlClobRepository _repository = new(TestConnectionString);
        private List<string> _documentsToCleanup = new();

        [TestInitialize]
        public void TestInitialize()
        {
            _repository = new SqlClobRepository(TestConnectionString);
            _documentsToCleanup = new List<string>();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            foreach (var docName in _documentsToCleanup)
            {
                try
                {
                    _repository.DeleteDocument(docName);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Cleanup error for {docName}: {ex.Message}");
                    // Ignore errors during cleanup as document might have been deleted by the test itself
                }
            }
             // Ensure FTS changes from deletions are processed if any test failed mid-operation
            Thread.Sleep(1000); // Adjusted sleep time for cleanup
        }

        private void AddTestDocument(string name, string content)
        {
            _repository.AddDocument(name, content);
            _documentsToCleanup.Add(name);
            Thread.Sleep(2500); // Increased sleep time for FTS indexing consistency
        }

        [TestMethod]
        [TestCategory("Integration")]
        public void AddDocument_AndVerifyRetrieval_ShouldSucceed()
        {
            string docName = $"AddRetrieveTest_{Guid.NewGuid()}";
            string docContent = "Content for add and retrieve test.";
            AddTestDocument(docName, docContent);

            // Search for terms known to be in the content.
            var results = _repository.SearchDocuments($"\"Content\" AND \"retrieve test.\""); 
            Assert.IsTrue(results.Any(d => d.Item1 == docName && d.Item2 == docContent), "Document not found or content mismatch after add.");
        }

        [TestMethod]
        [TestCategory("Integration")]
        public void DeleteDocument_ShouldRemoveFromDatabase()
        {
            string docName = $"DeleteTest_{Guid.NewGuid()}";
            string docContent = "Content for delete test.";
            AddTestDocument(docName, docContent);

            _repository.DeleteDocument(docName);
            _documentsToCleanup.Remove(docName); // Already deleted by test
            Thread.Sleep(2500); // Allow time for FTS indexing to reflect deletion

            var results = _repository.SearchDocuments($"\"{docName.Split('_')[0]}\"");
            Assert.IsFalse(results.Any(d => d.Item1 == docName), "Document found after deletion.");
        }

        [TestMethod]
        [TestCategory("Integration")]
        public void SearchDocumentsByContent_SingleTerm_ShouldReturnMatchingDocuments()
        {
            string term = $"UniqueTerm_{Guid.NewGuid().ToString("N")}";
            string docName1 = $"ContentSingle1_{Guid.NewGuid()}";
            string docContent1 = $"This document contains the {term}.";
            string docName2 = $"ContentSingle2_{Guid.NewGuid()}";
            string docContent2 = "This document does not.";
            AddTestDocument(docName1, docContent1);
            AddTestDocument(docName2, docContent2);

            var results = _repository.SearchDocuments($"\"{term}\"");
            Assert.IsTrue(results.Any(d => d.Item1 == docName1), "Document with the term was not found.");
            Assert.IsFalse(results.Any(d => d.Item1 == docName2), "Document without the term was found.");
        }

        [TestMethod]
        [TestCategory("Integration")]
        public void SearchDocumentsByContent_Phrase_ShouldReturnExactMatches()
        {
            string phrase = $"exact phrase test {Guid.NewGuid().ToString("N")}";
            string docName1 = $"ContentPhrase1_{Guid.NewGuid()}";
            string docContent1 = $"This document contains the {phrase}.";
            string docName2 = $"ContentPhrase2_{Guid.NewGuid()}";
            string docContent2 = $"This document has exact but not the phrase and also test {Guid.NewGuid().ToString("N")}.";
            AddTestDocument(docName1, docContent1);
            AddTestDocument(docName2, docContent2);

            var results = _repository.SearchDocuments($"\"{phrase}\"");
            Assert.IsTrue(results.Any(d => d.Item1 == docName1), "Document with the exact phrase was not found.");
            Assert.IsFalse(results.Any(d => d.Item1 == docName2), "Document without the exact phrase was found.");
        }

        [TestMethod]
        [TestCategory("Integration")]
        public void SearchDocumentsByContent_BooleanAND_ShouldReturnCorrectResults()
        {
            string term1 = $"AndTermA_{Guid.NewGuid().ToString("N")}"; // Changed for uniqueness
            string term2 = $"AndTermB_{Guid.NewGuid().ToString("N")}"; // Changed for uniqueness
            string docName1 = $"ContentAnd1_{Guid.NewGuid()}";
            string docContent1 = $"Contains {term1} and {term2}.";
            string docName2 = $"ContentAnd2_{Guid.NewGuid()}";
            string docContent2 = $"Contains only {term1}.";
            string docName3 = $"ContentAnd3_{Guid.NewGuid()}";
            string docContent3 = $"Contains only {term2}.";
            AddTestDocument(docName1, docContent1);
            AddTestDocument(docName2, docContent2);
            AddTestDocument(docName3, docContent3);

            var results = _repository.SearchDocuments($"\"{term1}\" AND \"{term2}\"");
            Assert.IsTrue(results.Count(d => d.Item1 == docName1) == 1, "Document with both terms not found or found multiple times.");
            Assert.IsFalse(results.Any(d => d.Item1 == docName2), "Document with only term1 found when ANDing.");
            Assert.IsFalse(results.Any(d => d.Item1 == docName3), "Document with only term2 found when ANDing.");
        }

        [TestMethod]
        [TestCategory("Integration")]
        public void SearchDocumentsByContent_BooleanOR_ShouldReturnCorrectResults()
        {
            string term1 = $"OrTermA_{Guid.NewGuid().ToString("N")}"; // Changed for uniqueness
            string term2 = $"OrTermB_{Guid.NewGuid().ToString("N")}"; // Changed for uniqueness
            string docName1 = $"ContentOr1_{Guid.NewGuid()}";
            string docContent1 = $"Contains {term1}.";
            string docName2 = $"ContentOr2_{Guid.NewGuid()}";
            string docContent2 = $"Contains {term2}.";
            string docName3 = $"ContentOr3_{Guid.NewGuid()}";
            string docContent3 = "Contains neither.";
            AddTestDocument(docName1, docContent1);
            AddTestDocument(docName2, docContent2);
            AddTestDocument(docName3, docContent3);

            var results = _repository.SearchDocuments($"\"{term1}\" OR \"{term2}\"");
            Assert.IsTrue(results.Any(d => d.Item1 == docName1), "Document with term1 not found for OR query.");
            Assert.IsTrue(results.Any(d => d.Item1 == docName2), "Document with term2 not found for OR query.");
            Assert.IsFalse(results.Any(d => d.Item1 == docName3), "Document with neither term found for OR query.");
            Assert.AreEqual(2, results.Count, "Incorrect number of documents found for OR query.");
        }

        [TestMethod]
        [TestCategory("Integration")]
        public void SearchDocumentsByContent_PrefixTerm_ShouldReturnMatchingDocuments()
        {
            string prefix = $"Prefix_{Guid.NewGuid().ToString("N")}";
            string docName1 = $"ContentPrefix1_{Guid.NewGuid()}";
            string docContent1 = $"This document has {prefix}CompleteWord.";
            string docName2 = $"ContentPrefix2_{Guid.NewGuid()}";
            string docContent2 = $"This document has {prefix}AnotherWord.";
            string docName3 = $"ContentPrefix3_{Guid.NewGuid()}";
            string docContent3 = "This document does not match the prefix.";
            AddTestDocument(docName1, docContent1);
            AddTestDocument(docName2, docContent2);
            AddTestDocument(docName3, docContent3);

            var results = _repository.SearchDocuments($"\"{prefix}*\"");
            Assert.IsTrue(results.Any(d => d.Item1 == docName1), "Document with first prefix match not found.");
            Assert.IsTrue(results.Any(d => d.Item1 == docName2), "Document with second prefix match not found.");
            Assert.IsFalse(results.Any(d => d.Item1 == docName3), "Document without prefix match found.");
            Assert.AreEqual(2, results.Count, "Incorrect number of documents found for prefix query.");
        }

        [TestMethod]
        [TestCategory("Integration")]
        public void SearchDocumentsByName_SingleTerm_ShouldReturnMatchingDocuments()
        {
            string uniqueTerm = $"NameTermUnique{Guid.NewGuid().ToString("N")}";
            string docName1 = uniqueTerm; // docName1 is now exactly the uniqueTerm
            string docContent1 = $"Content for {docName1}";
            string docName2 = $"OtherNameBeta_{Guid.NewGuid().ToString("N")}"; // Clearly distinct name
            string docContent2 = "Another content for beta.";
            AddTestDocument(docName1, docContent1);
            AddTestDocument(docName2, docContent2);

            var results = _repository.SearchDocumentsByName($"\"{uniqueTerm}\""); // Search for the exact uniqueTerm
            Assert.IsTrue(results.Any(d => d.Item1 == docName1), $"Document with name '{docName1}' (matching search term '{uniqueTerm}') was not found.");
            Assert.IsFalse(results.Any(d => d.Item1 == docName2), $"Document '{docName2}' (which should not match term '{uniqueTerm}') was found unexpectedly.");
        }

        [TestMethod]
        [TestCategory("Integration")]
        public void SearchDocumentsByName_Phrase_ShouldReturnExactMatches()
        {
            string uniquePhrase = $"Exact Name Phrase Search {Guid.NewGuid().ToString("N")}";
            string docName1 = $"{uniquePhrase}_DocGamma"; // Simplified name structure
            string docContent1 = $"Content for {docName1}";
            // Ensure docName2 does not contain uniquePhrase or its significant parts
            string docName2 = $"DifferentNameDelta_{Guid.NewGuid().ToString("N")} NotThePhrase"; 
            string docContent2 = "Another content for delta.";
            AddTestDocument(docName1, docContent1);
            AddTestDocument(docName2, docContent2);

            var results = _repository.SearchDocumentsByName($"\"{uniquePhrase}*\"");
            Assert.IsTrue(results.Any(d => d.Item1 == docName1), $"Document with the phrase '{uniquePhrase}' in name was not found.");
            Assert.IsFalse(results.Any(d => d.Item1 == docName2), $"Document '{docName2}' without the phrase '{uniquePhrase}' in name was found unexpectedly.");
        }

        [TestMethod]
        [TestCategory("Integration")]
        public void GetAllDocumentNames_ShouldReturnAllAddedDocumentNames()
        {
            string docName1 = $"GetAllTest1_{Guid.NewGuid()}";
            string docName2 = $"GetAllTest2_{Guid.NewGuid()}";
            AddTestDocument(docName1, "Content 1");
            AddTestDocument(docName2, "Content 2");

            var allNames = _repository.GetAllDocumentNames();
            Assert.IsTrue(allNames.Contains(docName1), $"{docName1} not found in GetAllDocumentNames.");
            Assert.IsTrue(allNames.Contains(docName2), $"{docName2} not found in GetAllDocumentNames.");
        }

        [TestMethod]
        [TestCategory("Integration")]
        public void SearchDocuments_NonExistentTerm_ShouldReturnEmptyList()
        {
            string term = $"NonExistentTerm_{Guid.NewGuid().ToString("N")}";
            AddTestDocument($"DummyDoc_{Guid.NewGuid()}", "Some content to ensure table is not empty.");
            
            var results = _repository.SearchDocuments($"\"{term}\"");
            Assert.AreEqual(0, results.Count, "Found documents for a non-existent term.");
        }

        [TestMethod]
        [TestCategory("Integration")]
        public void SearchDocumentsByName_NonExistentTerm_ShouldReturnEmptyList()
        {
            string term = $"NonExistentNameTerm_{Guid.NewGuid().ToString("N")}";
            AddTestDocument($"DummyDocName_{Guid.NewGuid()}", "Some content.");

            var results = _repository.SearchDocumentsByName($"\"{term}\"");
            Assert.AreEqual(0, results.Count, "Found documents for a non-existent name term.");
        }

        [TestMethod]
        [TestCategory("Integration")]
        public void AddDocument_EmptyName_ShouldThrowArgumentException()
        {
            Assert.ThrowsException<ArgumentException>(() => _repository.AddDocument("", "content"));
        }

        [TestMethod]
        [TestCategory("Integration")]
        public void AddDocument_NullContent_ShouldThrowArgumentNullException()
        {
            Assert.ThrowsException<ArgumentNullException>(() => _repository.AddDocument("TestDoc", null!));
        }

        [TestMethod]
        [TestCategory("Integration")]
        public void SearchDocuments_EmptyQuery_ShouldThrowArgumentException()
        {
            Assert.ThrowsException<ArgumentException>(() => _repository.SearchDocuments(""));
        }

        [TestMethod]
        [TestCategory("Integration")]
        public void SearchDocumentsByName_EmptyQuery_ShouldThrowArgumentException()
        {
            Assert.ThrowsException<ArgumentException>(() => _repository.SearchDocumentsByName(""));
        }
    }
}
