# ecom-platform

Basic functionality of an e-commerce platform. A user can be created by calling a REST API. After the user is created a message is sent to a RabbitMQ message broker. The consumer microservice gets the message from a queue and sends a mail notification to the user.

The following schema represents the structure of the microservices.
![Schema drawio](https://user-images.githubusercontent.com/44500761/170884035-1da2a8f5-ad90-41f9-b543-1438a1c56643.png)

## Getting started
1. Clone the repository 
2. Run `docker-compose build` inside the root directory
3. Run `docker-compose up`

## Posting data
You can test the microservices by performing the following `POST` request:
endpoint: `localhost:5000/api/user`
body: 
```json
{
    "FirstName": "Jon",
    "LastName": "Doe",
    "EmailAddress": "jon.doe@contoso.com",
    "Address": "Somewhere",
    "isEnrolled": true,
    "title": "B.Sc."
}
```

## Accessing RabbitMQ and SMTP server
- You can access the RabbitMQ management interface by accessing `localhost:15672` and entering `admin` as user and password
- You can access the SMTP server to see all incoming mails by accessing `localhost:8080` and entering
