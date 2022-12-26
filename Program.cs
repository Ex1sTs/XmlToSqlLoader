using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Xml;
using System.Globalization;

namespace testCase
{
    class Program
    {
        static void Main()
        {
            Console.WriteLine("Введите путь к XML файлу");
            string pathToXml = Console.ReadLine(); // C:\Users\Aleksey\Desktop\1\111.xml
            var reader = XmlReader.Create(@"" + pathToXml);
            reader.ReadToDescendant("orders");
            reader.ReadToDescendant("order");
            SqlConnection connection;
            SqlCommand command;
            List<string> listCommands = new List<string>();
            Console.WriteLine("Укажите SQL server");
            string sqlServerName = Console.ReadLine(); //DESKTOP-NO0EBSE
            Console.WriteLine("Укажите название БД");
            string bdName = Console.ReadLine(); //testCase
            string connectionString = @"Data Source=" + sqlServerName + ";Initial Catalog=" + bdName + ";Trusted_Connection=True"; 
            connection = new SqlConnection(connectionString);
            
            connection.Open();

            do
            {
                XmlDocument doc = new XmlDocument();
                doc.LoadXml(reader.ReadOuterXml());
                XmlNode item = doc.DocumentElement;

                int numberOrder =0;
                string reg_date ="";
                double sum =0;
                string sqlForOrder;
                string email = "";
                int userId = 0;
                
                foreach (XmlNode node in item)
                {
                    if (node.Name == "no" )
                    {
                        numberOrder = int.Parse(node.InnerText);
                        if (CheckIdSql(connection, "no", "orders", "no", numberOrder.ToString()) != -1 )
                        {
                            break;
                        }
                        
                    }

                    else if (node.Name == "reg_date")
                    {
                        if (string.IsNullOrEmpty(node.InnerText)) throw new ArgumentException("Order registration date is null or empty");
                        reg_date = node.InnerText;
                        if (!TextIsDate(reg_date)) throw new ArgumentException("incorrect date entered");
                    }

                    else if (node.Name == "sum")
                    {
                        if (string.IsNullOrEmpty(node.InnerText)) throw new ArgumentException("The order amount is null or empty");
                        bool sucsess = double.TryParse(node.InnerText, out double res);
                        if (sucsess)
                        {
                            sum = res;
                        }
                        else
                        {
                            sum = double.Parse(node.InnerText, CultureInfo.InvariantCulture);
                        }
                    }

                    else if (node.Name == "product")
                    {
                        int quantity = 0;
                        string name = "";
                        double price = 0;
                        string sqlForProduct;
                        string sqlForGoods;
                        foreach (XmlNode childNode in node)
                        {
                            if (childNode.Name == "quantity")
                            {
                                if (string.IsNullOrEmpty(childNode.InnerText)) throw new ArgumentException("the quantity of the product is null or empty");
                                quantity = int.Parse(childNode.InnerText);
                            }
                            else if (childNode.Name == "name")
                            {
                                if (string.IsNullOrEmpty(childNode.InnerText)) throw new ArgumentException("Product name null or empty");
                                name = childNode.InnerText.ToString();
                            }
                            else if (childNode.Name == "price")
                            {
                                if (string.IsNullOrEmpty(childNode.InnerText)) throw new ArgumentException("the price of the product is null or empty");
                                bool sucsess = double.TryParse(childNode.InnerText, out double res);
                                if (sucsess)
                                {
                                    price = res;
                                }
                                else
                                {
                                    price = double.Parse(childNode.InnerText, CultureInfo.InvariantCulture);
                                }
                            }
                        }

                        int productId = CheckIdSql(connection, "id", "products", "name", name);

                        if (productId == -1)
                        {
                            sqlForProduct = "insert into products (name, price) values('" + name + "', '" + price.ToString(CultureInfo.InvariantCulture) + "');";
                            command = new SqlCommand(sqlForProduct, connection);
                            command.ExecuteNonQuery();
                            productId = CheckIdSql(connection, "id", "products", "name", name);
                        }

                        sqlForGoods = "insert into goodsList (productId, orderNo, quantity) values('" + productId + "', '" + numberOrder + "', '" + quantity + "');";

                        listCommands.Add(sqlForGoods);
                    }
                    else if (node.Name == "user")
                    {
                        string fio ="";
                        string sqlForUser;
                        foreach (XmlNode childNode in node)
                        {
                            if (childNode.Name == "fio")
                            {
                                if (string.IsNullOrEmpty(childNode.InnerText)) throw new ArgumentException("fio of user is null or empty");
                                fio = childNode.InnerText.ToString();
                            }

                            else if (childNode.Name == "email")
                            {
                                if (string.IsNullOrEmpty(childNode.InnerText)) throw new ArgumentException("email of user is null or empty");
                                email = childNode.InnerText.ToString();
                            }
                        }

                        userId = CheckIdSql(connection, "id", "users", "email", email);
                        if (userId == -1)
                        {
                            sqlForUser = "insert into users (fio, email) values('" + fio + "', '" + email + "');";
                            command = new SqlCommand(sqlForUser, connection);
                            command.ExecuteNonQuery();
                            userId = CheckIdSql(connection, "id", "users", "email", email);
                        }
                    }
                }

                int currentOrderId = CheckIdSql(connection, "no", "orders", "no", numberOrder.ToString());
                if (currentOrderId == -1)
                {
                    command = new SqlCommand("SET IDENTITY_INSERT orders ON", connection);
                    command.ExecuteNonQuery();
                    sqlForOrder = "insert into orders (no, data, userId, sum) values('" + numberOrder + "', '" + reg_date + "', '" + userId + "', '" + sum.ToString(CultureInfo.InvariantCulture) + "');";
                    command = new SqlCommand(sqlForOrder, connection);
                    command.ExecuteNonQuery();
                    command = new SqlCommand("SET IDENTITY_INSERT orders OFF", connection);
                    command.ExecuteNonQuery();
                }
            }
            while (reader.ReadToNextSibling("order"));

            reader.Close();
            
            foreach (string sqlCommand in listCommands)
            {
                command = new SqlCommand(sqlCommand, connection);
                command.ExecuteNonQuery();
            }
            connection.Close();
            Console.WriteLine("Программа завершена");
            Console.ReadLine();
        }

        static int CheckIdSql(SqlConnection con, string idColumn, string tableName, string conditionColumn ,string condition)
        {
            string sqlCheck = "SELECT " + idColumn + " FROM " + tableName + " WHERE " + conditionColumn + " = '" + condition + "'";
            var checkCommand = new SqlCommand(sqlCheck, con);
            return (checkCommand.ExecuteScalar() == null) ? -1 : int.Parse(checkCommand.ExecuteScalar().ToString());
        }

        static bool TextIsDate(string text)
        {
            var dateFormat = "yyyy-MM-dd";
            DateTime scheduleDate;
            if (DateTime.TryParseExact(text, dateFormat, DateTimeFormatInfo.InvariantInfo, DateTimeStyles.None, out scheduleDate))
            {
                return true;
            }
            return false;
        }
    }
}
