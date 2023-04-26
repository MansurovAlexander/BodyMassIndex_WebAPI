using Microsoft.AspNetCore.Mvc;
using Npgsql;

namespace WebApi_BMI.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class BodyMassIndexController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        public BodyMassIndexController(IConfiguration configuration)
        {
            _configuration = configuration;
        }
        [HttpGet]
        public object BodyMassIndexGet()//Метод нахождения процентного соотношения людей с разным ИМТ
        {
            List<double> count = new List<double>(), conditionID = new List<double>();//count-количество людей из одной группы ИМТ, conditionID-ID описания ИМТ
            int countOfAll=1;//общее количество людей в БД
            string queryLower = @"select COUNT(userid) from users where conditionid_foreignkey=1";//реляционные запросы поиска количества строк с определенной группой ИМТ
            string queryNormal = @"select COUNT(userid) from users where conditionid_foreignkey=2";//1- недостаток веса, 2- норма, 3-ожирение
            string queryHigher = @"select COUNT(userid) from users where conditionid_foreignkey=3";
            string queryAll = @"select COUNT(userid) from users";//общее количество записей в таблице users
            string sqlDataSource = _configuration.GetConnectionString("HumanDB");//путь к серверу БД
            NpgsqlDataReader reader;
            using (NpgsqlConnection connection = new NpgsqlConnection(sqlDataSource))
            {
                connection.Open();//подключаюсь к серверу
                using (NpgsqlCommand command = new NpgsqlCommand(queryLower,connection))
                {
                    reader = command.ExecuteReader();//выполняю запрос
                    if (reader.HasRows)
                    {
                        reader.Read();
                        count.Add(double.Parse(string.Format("{0}", reader[0])));//получаю один стобец с количеством
                    }
                    conditionID.Add(1);//добавляю ID описания ИМТ
                    reader.Close();
                }
                using (NpgsqlCommand command = new NpgsqlCommand(queryNormal, connection))//повторяю для каждого запроса
                {
                    reader = command.ExecuteReader();
                    if (reader.HasRows)
                    {
                        reader.Read();
                        count.Add(double.Parse(string.Format("{0}", reader[0])));
                    }
                    conditionID.Add(2);
                    reader.Close();
                }
                using (NpgsqlCommand command = new NpgsqlCommand(queryHigher, connection))
                {
                    reader = command.ExecuteReader();
                    if (reader.HasRows)
                    {
                        reader.Read();
                        count.Add(double.Parse(string.Format("{0}", reader[0])));
                    }
                    conditionID.Add(3);
                    reader.Close();
                }
                using (NpgsqlCommand command = new NpgsqlCommand(queryAll, connection))
                {
                    reader = command.ExecuteReader();
                    if (reader.HasRows)
                    {
                        reader.Read();
                        countOfAll = int.Parse(string.Format("{0}", reader[0]));
                    }
                    reader.Close();
                }
            }
            count[0] /= countOfAll;//считаю количество в процентном соотношении
            count[1] /= countOfAll;
            count[2] /= countOfAll;
            if (count[0] < count[1])//сортирую по убыванию
            {
                Swap(count, 0, 1);
                Swap(conditionID, 0, 1);
            }
            else if (count[1] < count[2])
            {
                Swap(count, 1, 2);
                Swap(conditionID, 1, 2);
            }
            else if (count[0] < count[1])
            {
                Swap(count, 0, 1);
                Swap(conditionID, 0, 1);
            }
            string answer = "";
            for (int i=0;i<count.Count;i++)//заполняю статистику
            {
                if (conditionID[i] == 1)
                    answer += "Недостаток веса - ";
                else if (conditionID[i] == 2)
                    answer += "Норма - ";
                else
                    answer += "Ожирение - ";
                answer += (count[i] * 100).ToString() + "%; ";
            }
            return answer;//вывожу статистику
        }
        private void Swap(List<double> list, int firstIndex, int secondIndex)//меняю элементы списка местами
        {
            double tmp = list[firstIndex];
            list[firstIndex] = list[secondIndex];
            list[secondIndex] = tmp;
        }
        private double BodyMassIndex(double height, double weight)//считаю индекс массы тела
        {
            height /= 100;
            object description = "";
            double bodyMassIndex = weight / Math.Pow(height, 2);
            if (bodyMassIndex < 7.4 || bodyMassIndex > 204)//минимальный и максимальный возможный ИМТ, которые смог найти
                return -1;//минимальный и максимальный ИМТ
            else
                return bodyMassIndex;
        }
        [HttpPost]
        public object BodyMassIndexPost(string lastName, string firstName, string middleName, int age, double height, double weight)
        {
            double bodyMassIndex = BodyMassIndex(height, weight);
            if (bodyMassIndex != -1)//если данные отправленные пользователем более менее адекватные, то заношу их в таблицу
            {
                string query = @"
                insert into Users(userlastname, username, usermiddlename, userage, userheight, userweight, userbodymassindex, conditionid_foreignkey)
                values (@LastName, @FirstName, @MiddleName, @Age, @Height, @Weight, @BodyMassIndex, @ConditionID)
            ";//запрос
                string sqlDataSource = _configuration.GetConnectionString("HumanDB");//путь к БД
                NpgsqlDataReader reader;
                using (NpgsqlConnection connection = new NpgsqlConnection(sqlDataSource))
                {
                    connection.Open();//подключаюсь к БД
                    using (NpgsqlCommand command = new NpgsqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("LastName", NpgsqlTypes.NpgsqlDbType.Text, lastName);//Добавляю данные в команду
                        command.Parameters.AddWithValue("FirstName", NpgsqlTypes.NpgsqlDbType.Text, firstName);
                        command.Parameters.AddWithValue("MiddleName", NpgsqlTypes.NpgsqlDbType.Text, middleName);
                        command.Parameters.AddWithValue("Age", NpgsqlTypes.NpgsqlDbType.Integer, age);
                        command.Parameters.AddWithValue("Height", NpgsqlTypes.NpgsqlDbType.Double, height);
                        command.Parameters.AddWithValue("Weight", NpgsqlTypes.NpgsqlDbType.Double, weight);
                        command.Parameters.AddWithValue("BodyMassIndex", NpgsqlTypes.NpgsqlDbType.Double, bodyMassIndex);
                        if (bodyMassIndex<18.5)
                            command.Parameters.AddWithValue("ConditionID", NpgsqlTypes.NpgsqlDbType.Integer, 1);
                        else if (bodyMassIndex>=18.5 && bodyMassIndex<=25)
                            command.Parameters.AddWithValue("ConditionID", NpgsqlTypes.NpgsqlDbType.Integer, 2);
                        else if (bodyMassIndex > 25)
                            command.Parameters.AddWithValue("ConditionID", NpgsqlTypes.NpgsqlDbType.Integer, 3);
                        reader = command.ExecuteReader();//выполняю запрос
                        reader.Close();
                        connection.Close();//закрываю подключение
                    }
                }
                return "Успешно загруженно!";
            }
            else
                return "Введены не верные данные!";
        }
    }
}
