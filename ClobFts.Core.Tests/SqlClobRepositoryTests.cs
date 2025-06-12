using Microsoft.VisualStudio.TestTools.UnitTesting;
using ClobFts.Core;
using Moq;
using Moq.Protected; // Required for Protected().Setup<>
using System.Data.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Data; // Required for CommandBehavior

namespace ClobFts.Core.Tests
{
    [TestClass]
    public class SqlClobRepositoryTests
    {
#nullable disable // Suppress CS8618 for mock fields initialized in TestInitialize
        private Mock<DbConnection> _mockConnection;
        private Mock<DbCommand> _mockCommand;
        private Mock<DbDataReader> _mockDataReader;
        private Mock<DbParameterCollection> _mockDbParameterCollection;
        private List<DbParameter> _parameterList;
        private SqlClobRepository _repository;
#nullable restore

        [TestInitialize]
        public void TestInitialize()
        {
            _mockConnection = new Mock<DbConnection>();
            _mockCommand = new Mock<DbCommand>();
            _mockDataReader = new Mock<DbDataReader>();
            _mockDbParameterCollection = new Mock<DbParameterCollection>();
            _parameterList = new List<DbParameter>();

            // Setup connection
            // _mockConnection.Setup(c => c.CreateCommand()).Returns(_mockCommand.Object); // Incorrect: CreateCommand is not virtual
            _mockConnection.Protected()
                           .Setup<DbCommand>("CreateDbCommand") // Setup the protected abstract method
                           .Returns(_mockCommand.Object);
            
            // Setup for synchronous Open
            _mockConnection.Setup(c => c.Open()).Verifiable();
            _mockConnection.Setup(c => c.Close()).Verifiable();

            // Setup command
            // _mockCommand.Setup(cmd => cmd.Parameters).Returns(_mockDbParameterCollection.Object); // This is the problematic line
            _mockCommand.Protected().Setup<DbParameterCollection>("DbParameterCollection").Returns(_mockDbParameterCollection.Object);
            
            // Setup for synchronous command execution
            _mockCommand.Setup(cmd => cmd.ExecuteNonQuery())
                        .Returns(1); // Default to 1 row affected
            
            // Mock the protected ExecuteDbDataReader method
            _mockCommand.Protected()
                        .Setup<DbDataReader>("ExecuteDbDataReader", ItExpr.IsAny<CommandBehavior>())
                        .Returns(_mockDataReader.Object);

            _mockDbParameterCollection.Setup(p => p.Add(It.IsAny<object>()))
                .Callback<object>(param => _parameterList.Add((DbParameter)param))
                .Returns(0); // DbParameterCollection.Add returns the index of the added item

            // _mockCommand.Setup(cmd => cmd.CreateParameter()).Returns(() =>
            _mockCommand.Protected().Setup<DbParameter>("CreateDbParameter").Returns(() =>
            {
                var mockParam = new Mock<DbParameter>();
                mockParam.SetupAllProperties();
                return mockParam.Object;
            });

            _repository = new SqlClobRepository(() => _mockConnection.Object);
        }

        // Return type changed to DbParameter? to address CS8603
        private DbParameter? GetParameter(string name)
        {
            return _parameterList.FirstOrDefault(p => p.ParameterName == name);
        }

        [TestMethod]
        public void AddDocument_ValidInput_ShouldExecuteCorrectSqlAndParameters()
        {
            // Arrange
            string docName = "TestDoc1";
            string docContent = "This is content.";
            string expectedSql = "INSERT INTO Documents (DocumentName, Content) VALUES (@DocumentName, @Content)";
            _mockCommand.Setup(cmd => cmd.ExecuteNonQuery()).Returns(1).Verifiable();

            // Act
            _repository.AddDocument(docName, docContent);

            // Assert
            _mockCommand.VerifySet(cmd => cmd.CommandText = expectedSql, Times.Once());
            
            var nameParam = GetParameter("@DocumentName");
            Assert.IsNotNull(nameParam, "Parameter @DocumentName not found.");
            Assert.AreEqual(docName, nameParam.Value, "Parameter @DocumentName has incorrect value.");

            var contentParam = GetParameter("@Content");
            Assert.IsNotNull(contentParam, "Parameter @Content not found.");
            Assert.AreEqual(docContent, contentParam.Value, "Parameter @Content has incorrect value.");
            
            _mockCommand.Verify(cmd => cmd.ExecuteNonQuery(), Times.Once());
            _mockConnection.Verify(c => c.Open(), Times.Once());
        }

        [TestMethod]
        public void DeleteDocument_ExistingDocument_ShouldExecuteCorrectSqlAndParameters()
        {
            // Arrange
            string docName = "TestDocToDelete";
            string expectedSql = "DELETE FROM Documents WHERE DocumentName = @DocumentName";
            _mockCommand.Setup(cmd => cmd.ExecuteNonQuery()).Returns(1).Verifiable();

            // Act
            _repository.DeleteDocument(docName);

            // Assert
            _mockCommand.VerifySet(cmd => cmd.CommandText = expectedSql, Times.Once());
            
            var nameParam = GetParameter("@DocumentName");
            Assert.IsNotNull(nameParam, "Parameter @DocumentName not found.");
            Assert.AreEqual(docName, nameParam.Value, "Parameter @DocumentName has incorrect value.");

            _mockCommand.Verify(cmd => cmd.ExecuteNonQuery(), Times.Once());
            _mockConnection.Verify(c => c.Open(), Times.Once());
        }

        [TestMethod]
        public void DeleteDocument_NonExistingDocument_ShouldThrowException()
        {
            // Arrange
            string docName = "NonExistent";
            _mockCommand.Setup(cmd => cmd.ExecuteNonQuery()).Returns(0);

            // Act & Assert
            var ex = Assert.ThrowsException<Exception>(() => _repository.DeleteDocument(docName));
            Assert.AreEqual($"Document with name \'{docName}\' not found or could not be deleted.", ex.Message);
            _mockConnection.Verify(c => c.Open(), Times.Once());
        }

        [TestMethod]
        public void SearchDocuments_ValidQuery_ShouldExecuteCorrectSqlAndReturnResults()
        {
            // Arrange
            string searchQuery = "test query";
            // Simplified and corrected FTS query string generation
            string replacedQuery = searchQuery.Replace("\"", "\"\""); // Replaces " with ""
            string expectedFtsQuery = $"\"{replacedQuery}\"";       // Wraps with " at start and end
            string expectedSql = "SELECT DocumentName FROM Documents WHERE CONTAINS(Content, @FtsQuery)";
            var expectedDocNames = new List<string> { "Doc1", "Doc2" };

            var readCallCount = 0;
            _mockDataReader.Setup(r => r.Read()) 
                           .Returns(() => readCallCount < expectedDocNames.Count)
                           .Callback(() => readCallCount++); 
            
            _mockDataReader.Setup(r => r.GetString(0))
                           .Returns(() => expectedDocNames[readCallCount - 1]);
            
            // Act
            var results = _repository.SearchDocuments(searchQuery);

            // Assert
            _mockCommand.VerifySet(cmd => cmd.CommandText = expectedSql, Times.Once());
            
            var queryParam = GetParameter("@FtsQuery");
            Assert.IsNotNull(queryParam, "Parameter @FtsQuery not found.");
            Assert.AreEqual(expectedFtsQuery, queryParam.Value, "Parameter @FtsQuery has incorrect value.");
            
            // Verify that the underlying protected method ExecuteDbDataReader was called with CommandBehavior.Default.
            // The SUT calls the public ExecuteReader(), which in turn calls this protected method.
            _mockCommand.Protected().Verify<DbDataReader>(
                "ExecuteDbDataReader", 
                Times.Once(), 
                ItExpr.Is<CommandBehavior>(behavior => behavior == CommandBehavior.Default)
            );
            
            _mockConnection.Verify(c => c.Open(), Times.Once());

            Assert.AreEqual(expectedDocNames.Count, results.Count, "Incorrect number of results.");
            CollectionAssert.AreEqual(expectedDocNames, results, "The returned collection of document names does not match the expected collection.");
        }
        
        [TestMethod]
        public void SearchDocuments_NoResults_ShouldReturnEmptyList()
        {
            // Arrange
            string searchQuery = "no match";
            _mockDataReader.Setup(r => r.Read()).Returns(false); 
            // This line was correct, the repository calls ExecuteReader() without CommandBehavior
            // _mockCommand.Setup(cmd => cmd.ExecuteReader(It.IsAny<CommandBehavior>())).Returns(_mockDataReader.Object);


            // Act
            var results = _repository.SearchDocuments(searchQuery);

            // Assert
            Assert.AreEqual(0, results.Count);
            _mockConnection.Verify(c => c.Open(), Times.Once());
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void AddDocument_EmptyName_ShouldThrowArgumentException()
        {
            _repository.AddDocument("", "content");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentNullException))]
        public void AddDocument_NullContent_ShouldThrowArgumentNullException()
        {
            // Use null! to address CS8625 if NRTs are enabled for the test project
            _repository.AddDocument("docName", null!); 
        }
        
        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void DeleteDocument_EmptyName_ShouldThrowArgumentException()
        {
            _repository.DeleteDocument("");
        }

        [TestMethod]
        [ExpectedException(typeof(ArgumentException))]
        public void SearchDocuments_EmptyQuery_ShouldThrowArgumentException()
        {
            _repository.SearchDocuments("");
        }
    }
}
