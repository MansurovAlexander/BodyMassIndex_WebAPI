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
            List<double> count = new List<double>();//count-количество людей из одной группы ИМТ, conditionID-ID описания ИМТ
            List<string> description= new List<string>();
            double countOfAll = 0;
            string query = @"SELECT * FROM (
            SELECT COUNT(users.userid) AS usersCount, conditiondescription FROM users
            JOIN usercondition ON users.conditionid_foreignkey = usercondition.conditionid
            GROUP BY usercondition.conditionid ORDER BY COUNT(users.userid) DESC
            ) foo";//запрос БД
            string sqlDataSource = _configuration.GetConnectionString("HumanDB");//путь к серверу БД
            NpgsqlDataReader reader;
            using (NpgsqlConnection connection = new NpgsqlConnection(sqlDataSource))
            {
                connection.Open();//подключаюсь к серверу
                using (NpgsqlCommand command = new NpgsqlCommand(query,connection))
                {
                    reader = command.ExecuteReader();//выполняю запрос
                    while (reader.Read())
                    {
                        count.Add(int.Parse(string.Format("{0}", reader[0])));//получаю один стобец с количеством
                        description.Add(string.Format("{0}", reader[1]));
                        countOfAll += count[count.Count - 1];
                    }
                    reader.Close();
                }
            }
            count[0] /= countOfAll;//считаю количество в процентном соотношении
            count[1] /= countOfAll;
            count[2] /= countOfAll;
            string answer = "";
            for (int i=0;i<count.Count;i++)//заполняю статистику
                answer += description[i] + " - " + (Math.Round(count[i]*100)).ToString() + "%; ";
            return answer;//вывожу статистику
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
