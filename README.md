# RabbitMqSender. Инструкция по запуску
1. Перейти в папку `cd RabbitMqSender` и запустить команду `docker-compose up -d`. 
2. Ждем пока рэббит, постгрес и сервис логирования запустятся и бд смигрируется
    ```
     - Network rabbitmqsender_sender-network  Created                                                                  0.1s
     - Container rabbitmq                     Healthy                                                                 37.2s
     - Container seq                          Started                                                                  4.3s
     - Container sender-db                    Healthy                                                                 64.6s
     - Container sender-api                   Started                                                                 65.1s
    ```
4. Открываем `http://localhost:7889/swagger` и запускаем единственный метод `/sendPayment`, передаем json как параметр
    ```
    {
    	"request": {
    		"id": 27454821037510912,
    		"document": {
    			"id": 27454820926361856,
    			"type": "INVOICE_PAYMENT"
    		}
    	},
    	"debitPart": {
    		"agreementNumber": "RUS01",
    		"accountNumber": "30109810000000000001",
    		"amount": 3442.79,
    		"currency": "810",
    		"attributes": {}
    	},
    	"creditPart": {
    		"agreementNumber": "RUS01",
    		"accountNumber": "30233810000000000001",
    		"amount": 3442.79,
    		"currency": "810",
    		"attributes": {}
    	},
    	"details": "РАСЧЕТ",
    	"bankingDate": "2023-07-26",
    	"attributes": {
    		"attribute": [
    			{
    				"code": "pack",
    				"attribute": "37"
    			}
    		]
    	}
    }
    ```
    и получаем статус 200 в ответ.
5. Тем временем можно посмотреть происходящие в логах `http://localhost:8020/` и увидеть ошибку обращения к несуществующему адресу `https://somesite/api/v1/invoice` а так же посмотреть на передаваемый xml и запись json в бд.
6. Затем зайти в psql
   ```
   docker exec -it sender-db psql -U dbuser -d senderdb
   ```
   и посмотреть на количество запросов сохраненных в бд 
    ```
    SELECT p."ReceivedAt", p."JsonMessage", ps."Status" FROM "Payments" as p
    inner join "PaymentStatus" as ps on p."PaymentStatusId" = ps."Id";
    ```
7. Если результат не вывелся то надо подождать, возможно httpClient не успел вернуть ошибку и продолжить логику сохраненния в бд, нужно попытаться снова через пол минуты.
