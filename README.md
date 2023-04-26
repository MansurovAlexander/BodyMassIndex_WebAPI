# BodyMassIndex_WebAPI
b. Вес – Макс 40 баллов. Доработать WebApi методом добавления пациента (ФИО, рост, вес, возраст), вызов через POST, метод автоматически вычисляет ИМТ для данного пациента и заносит информацию в Базу данных (спроектировать структуру самостоятельно). Метод должен проверять входные параметры, в случае ошибки возвращать правильный статус ошибки. К решению приложить запросы из Postman с корректными и некорректными параметрами. Приложить ER-диаграмму БД.
![BodyMassIndex_ER](https://user-images.githubusercontent.com/107881836/234621037-90e71c72-fcdd-469b-b1e7-62662923f767.jpg)
Правильные запросы:\n
POST https://localhost:7182/BodyMassIndex?lastName=Ivanov&firstName=Fedor&middleName=Fedorovich&age=22&height=180&weight=72 // Все данные корректны, ИМТ в возможных пределах\n
POST https://localhost:7182/BodyMassIndex?lastName=Levin&firstName=Fedor&middleName=null&age=22&height=176&weight=65 // Все данные верны, у пользователя нет отчества\n

Запросы с ошибками:\n
POST https://localhost:7182/BodyMassIndex?firstName=Ivan&middleName=Ivanovich&age=22&height=182&weight=80 // Не все параметры\n
POST https://localhost:7182/BodyMassIndex?firstName=Ivan&age=22&height=182&weight=80\n
POST https://localhost:7182/BodyMassIndex?lastName=Ivanov&firstName=Fedor&middleName=Fedorovich&age=22&height=240&weight=40 // не адекватные данные\n

c. Вес – Макс 50 баллов. Доработать WebApi методом получения статистики по параметрам ИМТ пациентов из базы данных, вызов через GET. Метод вычисляет статистику посредством SQL-запроса и возвращает список характеристик ИМТ и процентное отношение клиентов в этой категории, по убыванию процентного соотношения, например: Норма – 70% Ниже нормы – 20% Ожирение – 10% Метод не принимает параметры. К решению приложить запросы из Postman, исходный код SQL-запроса.\n
Запросы:\n
GET https://localhost:7182/BodyMassIndex\n
SQL:\n
SELECT * FROM (
    SELECT COUNT(users.userid) AS usersCount, conditiondescription FROM users
    JOIN usercondition ON users.conditionid_foreignkey = usercondition.conditionid
    GROUP BY usercondition.conditionid ORDER BY COUNT(users.userid) DESC
) foo
