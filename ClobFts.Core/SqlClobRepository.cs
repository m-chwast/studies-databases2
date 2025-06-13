using System;
using System.Collections.Generic;
using System.Data.Common; // For DbConnection, DbCommand, etc.
using System.Data.SqlClient; // Still needed for the public constructor

namespace ClobFts.Core
{
    public class SqlClobRepository : IClobRepository
    {
        private readonly Func<DbConnection> _connectionFactory;

        // Public constructor for normal use
        public SqlClobRepository(string connectionString)
        {
            if (string.IsNullOrWhiteSpace(connectionString))
                throw new ArgumentNullException(nameof(connectionString));

            // For normal operation, we create SqlConnection instances.
            _connectionFactory = () => {
                var conn = new SqlConnection(connectionString);
                // It's important that the connection is returned closed.
                // The methods using it will open/close it.
                return conn;
            };
        }

        // Internal constructor for testing with a mocked connection factory
        public SqlClobRepository(Func<DbConnection> connectionFactory)
        {
            _connectionFactory = connectionFactory ?? throw new ArgumentNullException(nameof(connectionFactory));
        }

        public void AddDocument(string documentName, string content)
        {
            if (string.IsNullOrWhiteSpace(documentName))
                throw new ArgumentException("Document name cannot be empty.", nameof(documentName));
            if (content == null)
                throw new ArgumentNullException(nameof(content));

            using (DbConnection connection = _connectionFactory())
            {
                connection.Open();
                string sql = "INSERT INTO Documents (DocumentName, Content) VALUES (@DocumentName, @Content)";
                using (DbCommand command = connection.CreateCommand())
                {
                    command.CommandText = sql;

                    DbParameter nameParam = command.CreateParameter();
                    nameParam.ParameterName = "@DocumentName";
                    nameParam.Value = documentName;
                    command.Parameters.Add(nameParam);

                    DbParameter contentParam = command.CreateParameter();
                    contentParam.ParameterName = "@Content";
                    contentParam.Value = content;
                    command.Parameters.Add(contentParam);

                    command.ExecuteNonQuery();
                }
            }
        }

        public void DeleteDocument(string documentName)
        {
            if (string.IsNullOrWhiteSpace(documentName))
                throw new ArgumentException("Document name cannot be empty.", nameof(documentName));

            using (DbConnection connection = _connectionFactory())
            {
                connection.Open();
                string sql = "DELETE FROM Documents WHERE DocumentName = @DocumentName";
                using (DbCommand command = connection.CreateCommand())
                {
                    command.CommandText = sql;

                    DbParameter nameParam = command.CreateParameter();
                    nameParam.ParameterName = "@DocumentName";
                    nameParam.Value = documentName;
                    command.Parameters.Add(nameParam);

                    int rowsAffected = command.ExecuteNonQuery();
                    if (rowsAffected == 0)
                    {
                        throw new Exception($"Document with name '{documentName}' not found or could not be deleted.");
                    }
                }
            }
        }

        public List<Tuple<string, string>> SearchDocuments(string searchQuery)
        {
            if (string.IsNullOrWhiteSpace(searchQuery))
                throw new ArgumentException("Search query cannot be empty.", nameof(searchQuery));

            var foundDocuments = new List<Tuple<string, string>>();
            string ftsQuery = $"\"{searchQuery.Replace("\"", "\"\"")}\""; // Ensure FTS query is correctly formatted

            using (DbConnection connection = _connectionFactory())
            {
                connection.Open();
                // Select both DocumentName and Content
                string sql = "SELECT DocumentName, Content FROM Documents WHERE CONTAINS(Content, @FtsQuery)";
                using (DbCommand command = connection.CreateCommand())
                {
                    command.CommandText = sql;

                    DbParameter queryParam = command.CreateParameter();
                    queryParam.ParameterName = "@FtsQuery";
                    queryParam.Value = ftsQuery;
                    command.Parameters.Add(queryParam);

                    using (DbDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            // Assuming DocumentName is the first column (index 0)
                            // and Content is the second column (index 1)
                            string documentName = reader.GetString(0);
                            string content = reader.GetString(1);
                            foundDocuments.Add(new Tuple<string, string>(documentName, content));
                        }
                    }
                }
            }
            return foundDocuments;
        }

        public List<Tuple<string, string>> SearchDocumentsByName(string documentNameQuery)
        {
            if (string.IsNullOrWhiteSpace(documentNameQuery))
                throw new ArgumentException("Document name query cannot be empty.", nameof(documentNameQuery));

            var foundDocuments = new List<Tuple<string, string>>();
            // Using FTS for document name search. 
            // The query needs to be formatted for CONTAINS, typically with double quotes for exact phrases or wildcards.
            // For simple substring-like search, a wildcard can be used, e.g., "*query*"
            // However, leading wildcards are often problematic or less efficient in FTS.
            // A common approach for prefix search is "query*"
            // For this example, let's assume we want to find names containing the query words.
            string ftsQuery = $"\"{documentNameQuery.Replace("\"", "\"\"")}\""; // Removed trailing space inside quotes

            using (DbConnection connection = _connectionFactory())
            {
                connection.Open();
                // Using CONTAINS on DocumentName for FTS
                string sql = "SELECT DocumentName, Content FROM Documents WHERE CONTAINS(DocumentName, @FtsQuery)";
                using (DbCommand command = connection.CreateCommand())
                {
                    command.CommandText = sql;

                    DbParameter queryParam = command.CreateParameter();
                    queryParam.ParameterName = "@FtsQuery"; // Changed parameter name for clarity, though not strictly necessary if it was @DocumentNameQuery
                    queryParam.Value = ftsQuery;
                    command.Parameters.Add(queryParam);

                    using (DbDataReader reader = command.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            string documentName = reader.GetString(0);
                            string content = reader.GetString(1);
                            foundDocuments.Add(new Tuple<string, string>(documentName, content));
                        }
                    }
                }
            }
            return foundDocuments;
        }
    }
}
