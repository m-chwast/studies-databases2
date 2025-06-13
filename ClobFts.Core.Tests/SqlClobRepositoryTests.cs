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
            string searchQuery = "test";
            string expectedFtsQuery = "\"test\"";
            var expectedResults = new List<Tuple<string, string>>
            {
                new Tuple<string, string>("doc1.txt", "This is a test document."),
                new Tuple<string, string>("doc2.txt", "Another test entry.")
            };

            _mockDataReader.SetupSequence(r => r.Read()) // Corrected: _mockDataReader instead of _mockReader
                       .Returns(true)
                       .Returns(true)
                       .Returns(false);
            _mockDataReader.Setup(r => r.GetString(0)) // Corrected: _mockDataReader instead of _mockReader
                       .Returns(expectedResults[0].Item1)
                       .Callback(() => _mockDataReader.Setup(r => r.GetString(0)).Returns(expectedResults[1].Item1)); // Corrected: _mockDataReader
            _mockDataReader.Setup(r => r.GetString(1)) // Corrected: _mockDataReader instead of _mockReader
                       .Returns(expectedResults[0].Item2)
                       .Callback(() => _mockDataReader.Setup(r => r.GetString(1)).Returns(expectedResults[1].Item2)); // Corrected: _mockDataReader

            _mockCommand.Protected().Setup<DbDataReader>("ExecuteDbDataReader", ItExpr.Is<CommandBehavior>(b => b == CommandBehavior.Default)).Returns(_mockDataReader.Object); // Corrected: _mockDataReader instead of _mockReader

            // Act
            var results = _repository.SearchDocuments(searchQuery);

            // Assert
            _mockCommand.VerifySet(cmd => cmd.CommandText = "SELECT DocumentName, Content FROM Documents WHERE CONTAINS(Content, @FtsQuery)", Times.Once()); // Verify CommandText is set
            _mockCommand.Protected().Verify("ExecuteDbDataReader", Times.Once(), ItExpr.Is<CommandBehavior>(behavior => behavior == CommandBehavior.Default));
            
            Assert.AreEqual(1, _parameterList.Count); // Use _parameterList populated by callback
            var param = _parameterList[0]; // Use _parameterList
            Assert.IsNotNull(param);
            Assert.AreEqual("@FtsQuery", param.ParameterName);
            Assert.AreEqual(expectedFtsQuery, param.Value);

            Assert.IsNotNull(results);
            Assert.AreEqual(expectedResults.Count, results.Count);
            for (int i = 0; i < expectedResults.Count; i++)
            {
                Assert.AreEqual(expectedResults[i].Item1, results[i].Item1);
                Assert.AreEqual(expectedResults[i].Item2, results[i].Item2);
            }
        }

        [TestMethod]
        public void SearchDocuments_EmptyQuery_ShouldThrowArgumentException()
        {
            // Arrange
            string searchQuery = string.Empty;

            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() => _repository.SearchDocuments(searchQuery));
        }

        [TestMethod]
        public void SearchDocuments_WhitespaceQuery_ShouldThrowArgumentException()
        {
            // Arrange
            string searchQuery = "   ";

            // Act & Assert
            Assert.ThrowsException<ArgumentException>(() => _repository.SearchDocuments(searchQuery));
        }

        [TestMethod]
        public void SearchDocuments_NoResults_ShouldReturnEmptyList()
        {
            // Arrange
            string searchQuery = "nonexistent";
            _mockDataReader.Setup(r => r.Read()).Returns(false); // Corrected: _mockDataReader instead of _mockReader
            _mockCommand.Protected().Setup<DbDataReader>("ExecuteDbDataReader", ItExpr.Is<CommandBehavior>(b => b == CommandBehavior.Default)).Returns(_mockDataReader.Object); // Corrected: _mockDataReader instead of _mockReader

            // Act
            var results = _repository.SearchDocuments(searchQuery);

            // Assert
            Assert.IsNotNull(results);
            Assert.AreEqual(0, results.Count);
        }

        [TestMethod]
        public void SearchDocuments_QueryWithQuotes_ShouldEscapeQuotesInFtsQuery()
        {
            // Arrange
            string searchQuery = "test \"quote\"";
            string expectedFtsQuery = "\"test \"\"quote\"\"\""; // FTS expects quotes to be doubled

            _mockDataReader.Setup(r => r.Read()).Returns(false); // Corrected: _mockDataReader instead of _mockReader
            _mockCommand.Protected().Setup<DbDataReader>("ExecuteDbDataReader", ItExpr.Is<CommandBehavior>(b => b == CommandBehavior.Default)).Returns(_mockDataReader.Object); // Corrected: _mockDataReader instead of _mockReader

            // Act
            _repository.SearchDocuments(searchQuery);

            // Assert
            _mockCommand.VerifySet(cmd => cmd.CommandText = "SELECT DocumentName, Content FROM Documents WHERE CONTAINS(Content, @FtsQuery)", Times.Once()); // Verify CommandText is set
            Assert.AreEqual(1, _parameterList.Count); // Use _parameterList populated by callback
            var param = _parameterList[0]; // Use _parameterList
            Assert.IsNotNull(param);
            Assert.AreEqual("@FtsQuery", param.ParameterName);
            Assert.AreEqual(expectedFtsQuery, param.Value);
        }
    }
}
