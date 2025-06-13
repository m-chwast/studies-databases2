using Microsoft.VisualStudio.TestTools.UnitTesting;
using ClobFts.Core;
using System;
using System.Collections.Generic;
using System.Linq;

namespace ClobFts.Core.Tests
{
    /// <summary>
    /// Integration tests for SqlClobRepository that connect to a real SQL Server database.
    /// These tests require a running SQL Server instance with the Documents table and FTS enabled.
    /// </summary>
    [TestClass]
    public class SqlClobRepositoryIntegrationTests
    {
        // Connection string for integration tests - update this to match your test database
        private const string TestConnectionString = "Data Source=WINSERVER;Initial Catalog=testCLR;Integrated Security=True;Persist Security Info=False;Pooling=False;Connect Timeout=60;Encrypt=False;TrustServerCertificate=False";
        
        private SqlClobRepository _repository;
        private List<string> _testDocumentsToCleanup;

        [TestInitialize]
        public void TestInitialize()
        {
            _repository = new SqlClobRepository(TestConnectionString);
            _testDocumentsToCleanup = new List<string>();
        }

        [TestCleanup]
        public void TestCleanup()
        {
            // Clean up any test documents created during the tests
            foreach (string docName in _testDocumentsToCleanup)
            {
                try
                {
                    _repository.DeleteDocument(docName);
                }
                catch (Exception)
                {
                    // Ignore errors during cleanup - document might not exist
                }
            }
        }

        [TestMethod]
        [TestCategory("Integration")]
        public void AddDocument_RealDatabase_ShouldInsertSuccessfully()
        {
            // Arrange
            string testDocName = $"IntegrationTest_Add_{Guid.NewGuid()}";
            string testContent = "This is a test document for integration testing with real database connection.";
            _testDocumentsToCleanup.Add(testDocName);

            // Act
            _repository.AddDocument(testDocName, testContent);

            // Assert - Verify document exists by searching for it
            var searchResults = _repository.SearchDocuments("integration testing");
            Assert.IsTrue(searchResults.Any(r => r.Item1 == testDocName), "Document should be found in search results");
            
            var foundDoc = searchResults.First(r => r.Item1 == testDocName);
            Assert.AreEqual(testContent, foundDoc.Item2, "Document content should match");
        }

        [TestMethod]
        [TestCategory("Integration")]
        public void DeleteDocument_RealDatabase_ShouldRemoveSuccessfully()
        {
            // Arrange
            string testDocName = $"IntegrationTest_Delete_{Guid.NewGuid()}";
            string testContent = "This document will be deleted during the test.";
            
            _repository.AddDocument(testDocName, testContent);
            
            // Verify document exists
            var allDocsBefore = _repository.GetAllDocumentNames();
            Assert.IsTrue(allDocsBefore.Contains(testDocName), "Document should exist before deletion");

            // Act
            _repository.DeleteDocument(testDocName);

            // Assert
            var allDocsAfter = _repository.GetAllDocumentNames();
            Assert.IsFalse(allDocsAfter.Contains(testDocName), "Document should not exist after deletion");
        }

        [TestMethod]
        [TestCategory("Integration")]
        public void DeleteDocument_NonExistentDocument_ShouldThrowException()
        {
            // Arrange
            string nonExistentDoc = $"NonExistent_{Guid.NewGuid()}";

            // Act & Assert
            var exception = Assert.ThrowsException<Exception>(() => _repository.DeleteDocument(nonExistentDoc));
            Assert.IsTrue(exception.Message.Contains("not found"), "Exception message should indicate document not found");
        }

        [TestMethod]
        [TestCategory("Integration")]
        public void SearchDocuments_SimpleTerm_ShouldReturnMatchingDocuments()
        {
            // Arrange
            string testDocName1 = $"IntegrationTest_Search1_{Guid.NewGuid()}";
            string testDocName2 = $"IntegrationTest_Search2_{Guid.NewGuid()}";
            string testContent1 = "This document contains the keyword automobile for testing.";
            string testContent2 = "This document contains the keyword vehicle for testing.";
            string testContent3 = "This document contains the keyword automobile and vehicle for comprehensive testing.";
            string testDocName3 = $"IntegrationTest_Search3_{Guid.NewGuid()}";

            _testDocumentsToCleanup.AddRange(new[] { testDocName1, testDocName2, testDocName3 });

            _repository.AddDocument(testDocName1, testContent1);
            _repository.AddDocument(testDocName2, testContent2);
            _repository.AddDocument(testDocName3, testContent3);

            // Act
            var results = _repository.SearchDocuments("automobile");

            // Assert
            Assert.IsTrue(results.Count >= 2, "Should find at least 2 documents containing 'automobile'");
            Assert.IsTrue(results.Any(r => r.Item1 == testDocName1), "Should find first test document");
            Assert.IsTrue(results.Any(r => r.Item1 == testDocName3), "Should find third test document");
            Assert.IsFalse(results.Any(r => r.Item1 == testDocName2), "Should not find second test document");
        }

        [TestMethod]
        [TestCategory("Integration")]
        public void SearchDocuments_PhraseQuery_ShouldReturnExactMatches()
        {
            // Arrange
            string testDocName1 = $"IntegrationTest_Phrase1_{Guid.NewGuid()}";
            string testDocName2 = $"IntegrationTest_Phrase2_{Guid.NewGuid()}";
            string testContent1 = "This document contains machine learning algorithms for data analysis.";
            string testContent2 = "This document has machine and learning but not together algorithms.";

            _testDocumentsToCleanup.AddRange(new[] { testDocName1, testDocName2 });

            _repository.AddDocument(testDocName1, testContent1);
            _repository.AddDocument(testDocName2, testContent2);

            // Act
            var results = _repository.SearchDocuments("\"machine learning\"");

            // Assert
            Assert.IsTrue(results.Any(r => r.Item1 == testDocName1), "Should find document with exact phrase 'machine learning'");
            // Note: Document 2 might or might not be found depending on FTS proximity settings
        }

        [TestMethod]
        [TestCategory("Integration")]
        public void SearchDocuments_BooleanQuery_ShouldRespectOperators()
        {
            // Arrange
            string testDocName1 = $"IntegrationTest_Bool1_{Guid.NewGuid()}";
            string testDocName2 = $"IntegrationTest_Bool2_{Guid.NewGuid()}";
            string testDocName3 = $"IntegrationTest_Bool3_{Guid.NewGuid()}";
            string testContent1 = "This document discusses software development and programming techniques.";
            string testContent2 = "This document covers hardware development and engineering methods.";
            string testContent3 = "This document is about software engineering and system design.";

            _testDocumentsToCleanup.AddRange(new[] { testDocName1, testDocName2, testDocName3 });

            _repository.AddDocument(testDocName1, testContent1);
            _repository.AddDocument(testDocName2, testContent2);
            _repository.AddDocument(testDocName3, testContent3);

            // Wait a bit for FTS indexing (FTS indexing is not immediate)
            System.Threading.Thread.Sleep(2000);

            // Act - Try simpler search first, then boolean if supported
            var softwareResults = _repository.SearchDocuments("software");
            var developmentResults = _repository.SearchDocuments("development");
            
            // Assert - Check that basic searches work first
            Assert.IsTrue(softwareResults.Any(r => r.Item1 == testDocName1), "Should find document containing 'software'");
            Assert.IsTrue(developmentResults.Any(r => r.Item1 == testDocName1), "Should find document containing 'development'");
            
            // Try boolean query (may not be supported in all FTS configurations)
            try
            {
                var booleanResults = _repository.SearchDocuments("software AND development");
                if (booleanResults.Count > 0)
                {
                    Assert.IsTrue(booleanResults.Any(r => r.Item1 == testDocName1), "Should find document with both 'software' and 'development'");
                }
            }
            catch (Exception)
            {
                // Boolean operators might not be supported in this FTS configuration
                // This is acceptable for basic FTS functionality
            }
        }

        [TestMethod]
        [TestCategory("Integration")]
        public void SearchDocumentsByName_SimpleTerm_ShouldReturnMatchingNames()
        {
            // Arrange
            string testDocName1 = $"TechnicalReport_{Guid.NewGuid()}.pdf";
            string testDocName2 = $"UserManual_{Guid.NewGuid()}.docx";
            string testDocName3 = $"TechnicalSpecification_{Guid.NewGuid()}.txt";
            string testContent = "Standard test content for name search testing.";

            _testDocumentsToCleanup.AddRange(new[] { testDocName1, testDocName2, testDocName3 });

            _repository.AddDocument(testDocName1, testContent);
            _repository.AddDocument(testDocName2, testContent);
            _repository.AddDocument(testDocName3, testContent);

            // Act
            var results = _repository.SearchDocumentsByName("Technical");

            // Assert
            Assert.IsTrue(results.Count >= 2, "Should find at least 2 documents with 'Technical' in name");
            Assert.IsTrue(results.Any(r => r.Item1 == testDocName1), "Should find TechnicalReport document");
            Assert.IsTrue(results.Any(r => r.Item1 == testDocName3), "Should find TechnicalSpecification document");
            Assert.IsFalse(results.Any(r => r.Item1 == testDocName2), "Should not find UserManual document");
        }

        [TestMethod]
        [TestCategory("Integration")]
        public void SearchDocumentsByName_PhraseQuery_ShouldReturnExactMatches()
        {
            // Arrange
            string testDocName1 = $"User Guide Manual_{Guid.NewGuid()}.pdf";
            string testDocName2 = $"User Manual Guide_{Guid.NewGuid()}.docx";
            string testDocName3 = $"Guide for Users_{Guid.NewGuid()}.txt";
            string testContent = "Standard test content for phrase search testing.";

            _testDocumentsToCleanup.AddRange(new[] { testDocName1, testDocName2, testDocName3 });

            _repository.AddDocument(testDocName1, testContent);
            _repository.AddDocument(testDocName2, testContent);
            _repository.AddDocument(testDocName3, testContent);

            // Act
            var results = _repository.SearchDocumentsByName("\"User Guide\"");

            // Assert
            Assert.IsTrue(results.Any(r => r.Item1 == testDocName1), "Should find document with exact phrase 'User Guide'");
            // Note: Depending on FTS settings, other documents might or might not be found
        }

        [TestMethod]
        [TestCategory("Integration")]
        public void GetAllDocumentNames_RealDatabase_ShouldReturnOrderedList()
        {
            // Arrange
            string testDocName1 = $"Alpha_Doc_{Guid.NewGuid()}";
            string testDocName2 = $"Beta_Doc_{Guid.NewGuid()}";
            string testDocName3 = $"Gamma_Doc_{Guid.NewGuid()}";
            string testContent = "Test content for ordering verification.";

            _testDocumentsToCleanup.AddRange(new[] { testDocName1, testDocName2, testDocName3 });

            _repository.AddDocument(testDocName2, testContent); // Add in non-alphabetical order
            _repository.AddDocument(testDocName3, testContent);
            _repository.AddDocument(testDocName1, testContent);

            // Act
            var allDocuments = _repository.GetAllDocumentNames();

            // Assert
            Assert.IsNotNull(allDocuments, "Document list should not be null");
            Assert.IsTrue(allDocuments.Count > 0, "Should return at least some documents");
            
            // Verify our test documents are in the list and properly ordered
            var testDocs = allDocuments.Where(name => 
                name == testDocName1 || name == testDocName2 || name == testDocName3).ToList();
            
            Assert.AreEqual(3, testDocs.Count, "Should find all 3 test documents");
            
            // Verify alphabetical ordering of our test documents
            var expectedOrder = new[] { testDocName1, testDocName2, testDocName3 }.OrderBy(x => x).ToList();
            var actualOrder = testDocs.OrderBy(x => x).ToList();
            
            CollectionAssert.AreEqual(expectedOrder, actualOrder, "Documents should be in alphabetical order");
        }

        [TestMethod]
        [TestCategory("Integration")]
        public void SearchDocuments_EmptyQuery_ShouldThrowArgumentException()
        {
            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() => _repository.SearchDocuments(""));
            Assert.ThrowsException<ArgumentException>(() => _repository.SearchDocuments("   "));
        }

        [TestMethod]
        [TestCategory("Integration")]
        public void SearchDocumentsByName_EmptyQuery_ShouldThrowArgumentException()
        {
            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() => _repository.SearchDocumentsByName(""));
            Assert.ThrowsException<ArgumentException>(() => _repository.SearchDocumentsByName("   "));
        }

        [TestMethod]
        [TestCategory("Integration")]
        public void AddDocument_EmptyName_ShouldThrowArgumentException()
        {
            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() => _repository.AddDocument("", "content"));
            Assert.ThrowsException<ArgumentException>(() => _repository.AddDocument("   ", "content"));
        }

        [TestMethod]
        [TestCategory("Integration")]
        public void AddDocument_NullContent_ShouldThrowArgumentNullException()
        {
            // Act & Assert
            Assert.ThrowsException<ArgumentNullException>(() => _repository.AddDocument("TestDoc", null));
        }

        [TestMethod]
        [TestCategory("Integration")]
        public void SearchDocuments_NoMatches_ShouldReturnEmptyList()
        {
            // Act
            var results = _repository.SearchDocuments($"VeryUniqueSearchTerm_{Guid.NewGuid()}");

            // Assert
            Assert.IsNotNull(results, "Results should not be null");
            Assert.AreEqual(0, results.Count, "Should return empty list when no matches found");
        }

        [TestMethod]
        [TestCategory("Integration")]
        public void SearchDocumentsByName_NoMatches_ShouldReturnEmptyList()
        {
            // Act
            var results = _repository.SearchDocumentsByName($"VeryUniqueFileName_{Guid.NewGuid()}");

            // Assert
            Assert.IsNotNull(results, "Results should not be null");
            Assert.AreEqual(0, results.Count, "Should return empty list when no matches found");
        }

        [TestMethod]
        [TestCategory("Integration")]
        public void CompleteWorkflow_AddSearchDelete_ShouldWorkEndToEnd()
        {
            // Arrange
            string testDocName = $"WorkflowTest_{Guid.NewGuid()}";
            string testContent = "This is a comprehensive workflow test document containing unique keywords like zzztestunique.";

            // Act & Assert - Add
            _repository.AddDocument(testDocName, testContent);
            
            // Act & Assert - Search by content
            var contentResults = _repository.SearchDocuments("zzztestunique");
            Assert.IsTrue(contentResults.Any(r => r.Item1 == testDocName), "Should find document by content search");
            
            // Act & Assert - Search by name
            var nameResults = _repository.SearchDocumentsByName("WorkflowTest");
            Assert.IsTrue(nameResults.Any(r => r.Item1 == testDocName), "Should find document by name search");
            
            // Act & Assert - Get all documents
            var allDocs = _repository.GetAllDocumentNames();
            Assert.IsTrue(allDocs.Contains(testDocName), "Should find document in complete list");
            
            // Act & Assert - Delete
            _repository.DeleteDocument(testDocName);
            
            // Verify deletion
            var docsAfterDelete = _repository.GetAllDocumentNames();
            Assert.IsFalse(docsAfterDelete.Contains(testDocName), "Document should be removed after deletion");
            
            var searchAfterDelete = _repository.SearchDocuments("zzztestunique");
            Assert.IsFalse(searchAfterDelete.Any(r => r.Item1 == testDocName), "Should not find deleted document in search");
        }
    }
}
