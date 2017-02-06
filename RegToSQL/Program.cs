using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Data.SQLite;
using System.Threading;
using Microsoft.Win32;
using System.IO;

namespace RegToSQL
{
    class Program
    {
        //количество добавленных в базу строк дерева, используется как ссылка на родителя.
        //в базе под 1 индексом пустая строка чтобы не нарушать целостность базы.
        static int count = 1;
        static string connStr = @"Data Source=regsql.db";
        static SQLiteConnection conn = new SQLiteConnection(connStr);
        
        //шаблон для записи ветвей реестра
        static SQLiteCommand cmdToReg = new SQLiteCommand(@"Insert into Reg(name, parent_id) 
            Values (@name, @parent_id)", conn);
        
        //шаблон для записи значений
        static SQLiteCommand cmdToValue = new SQLiteCommand(@"INSERT INTO Value
               (parent_id
               ,name
               ,type
               ,value)
               Values (@parent_id, @name, @type, @value)", conn);


        static void Main(string[] args)
        {
            //удалить базу если она была создана ранее
            if (File.Exists("regsql.db"))
            {
                File.Delete("regsql.db");
            }

            //выгрузить базу из ресурсов
            File.WriteAllBytes("regsql.db", RegToSQL.Properties.Resources.regsql);

            Console.WriteLine("Нажмите любую клавишу для начала работы");
            Console.ReadKey(true);

            //подсчет времени работы программы
            System.Diagnostics.Stopwatch myStopwatch = new System.Diagnostics.Stopwatch();
            myStopwatch.Start();

            try
            {
                conn.Open();                
            }
            catch (SQLiteException se)
            {
                Console.WriteLine("Ошибка подключения: {0}", se.Message);
                return;
            }

            Console.WriteLine();
            Console.WriteLine("Соедение установлено");
            Console.WriteLine("Выполняется вставка");

            var transaction = conn.BeginTransaction();

            RegistryKey Reg = Registry.ClassesRoot;
            InsertTree(0,count, Reg);

            Reg = Registry.CurrentUser;
            InsertTree(0, count, Reg);

            Reg = Registry.LocalMachine;
            InsertTree(0, count, Reg);

            Reg = Registry.Users;
            InsertTree(0, count, Reg);

            Reg = Registry.CurrentConfig;
            InsertTree(0, count, Reg);

            transaction.Commit();
            conn.Close();

            myStopwatch.Stop();

            Console.WriteLine();
            Console.WriteLine("Работа завершена время выполнения: {0}", myStopwatch.Elapsed);
            Console.WriteLine("количество добавленных записей: {0}", count);
            Console.WriteLine();

            Console.WriteLine("Нажмите любую клавишу для завершения");
            Console.ReadKey(true);
        }

        //Вставка ветвей реестра
        static void InsertTree(int parent_id, int id, RegistryKey thisKey)
        {
            int thisKeyID = id;

            List<String> tmp = new List<string>(thisKey.Name.Split('\\'));
            String name = tmp.Last();


            SQLiteParameter param = new SQLiteParameter();
            param.ParameterName = "id";
            param.Value = thisKeyID;
            cmdToReg.Parameters.Add(param);

            param = new SQLiteParameter();
            param.ParameterName = "@parent_id";
            param.Value = parent_id;
            cmdToReg.Parameters.Add(param);

            param = new SQLiteParameter();
            param.ParameterName = "@name";
            param.Value = name;
            cmdToReg.Parameters.Add(param);

            try
            {
                cmdToReg.ExecuteNonQuery();

                count++;                

                InsertValue(thisKeyID, thisKey);

                //рекурсивное составление ветки
                foreach (var item in thisKey.GetSubKeyNames())
                {
                    InsertTree(thisKeyID, count, thisKey.OpenSubKey(item));
                }
            }
            catch (Exception ex) 
            {
                Console.WriteLine("Ошибка в {0} : {1}", name, ex.Message);
            }
        }

        //вставка значений ветвей.
        static void InsertValue(int parent_id, RegistryKey thisKey)
        {
            foreach (var item in thisKey.GetValueNames())
            {
                
               SQLiteParameter param = new SQLiteParameter();
               param.ParameterName = "@parent_id";
               param.Value = parent_id;
               cmdToValue.Parameters.Add(param);

               param = new SQLiteParameter();
               param.ParameterName = "@name";
               param.Value = item;
               cmdToValue.Parameters.Add(param);

               param = new SQLiteParameter();
               param.ParameterName = "@type";
               param.Value = thisKey.GetValueKind(item).ToString();
               cmdToValue.Parameters.Add(param);

               param = new SQLiteParameter();
               param.ParameterName = "@value";
               param.Value = thisKey.GetValue(item).ToString();
               cmdToValue.Parameters.Add(param);

               try
               {
                   cmdToValue.ExecuteNonQuery();
               }
               catch (Exception ex)
               {
                   Console.WriteLine("Ошибка в {0} : {1}", thisKey.Name, ex.Message);
               }

            }
        }
    }    
}
