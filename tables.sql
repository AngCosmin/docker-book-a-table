CREATE TABLE users(
    id INT PRIMARY KEY NOT NULL, 
    name VARCHAR(30) NOT NULL, 
    email VARCHAR(80) NOT NULL, 
    phone VARCHAR(20), 
    password VARCHAR(50) NOT NULL, 
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP
);

CREATE TABLE users(id INT PRIMARY KEY NOT NULL, name VARCHAR(30) NOT NULL, email VARCHAR(80) NOT NULL, phone VARCHAR(20), password VARCHAR(50) NOT NULL, created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP);

CREATE TABLE restaurants(
    id INT PRIMARY KEY NOT NULL, 
    name VARCHAR(60) NOT NULL, 
    email VARCHAR(80) NOT NULL, 
    phone VARCHAR(20), 
    password VARCHAR(50) NOT NULL, 
    no_places INT NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP, 
);

CREATE TABLE restaurants(id INT PRIMARY KEY NOT NULL, name VARCHAR(60) NOT NULL, email VARCHAR(80) NOT NULL, phone VARCHAR(20), password VARCHAR(50) NOT NULL, no_places INT NOT NULL, logo VARCHAR(255), created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP);

INSERT INTO restaurants VALUES (1, 'Oscar', 'oscar@gmail.com', '0728441126', 'oscar', 20, 'http://www.restaurantoscar.ro/img/restaurant/1.jpg', CURRENT_TIMESTAMP);
INSERT INTO restaurants VALUES (2, 'Milia', 'milia@gmail.com', '0728441127', 'milia', 10, 'https://scontent.fotp3-1.fna.fbcdn.net/v/t1.0-9/21740526_772983509568833_452600469297350631_n.jpg?_nc_cat=111&_nc_ht=scontent.fotp3-1.fna&oh=6521c06accafb5ce6353e073a499a4d5&oe=5CCBCB2B', CURRENT_TIMESTAMP);
INSERT INTO restaurants VALUES (3, 'Star', 'star@gmail.com', '0728441128', 'star', 5, 'https://scontent.fotp3-1.fna.fbcdn.net/v/t1.0-9/11059322_827093123994184_7802084377421279853_n.jpg?_nc_cat=111&_nc_ht=scontent.fotp3-1.fna&oh=94c2eec4edfa453ae2433970ada6b0e5&oe=5CCE622C', CURRENT_TIMESTAMP);
INSERT INTO restaurants VALUES (4, 'Dominos', 'dominos@gmail.com', '0728441129', 'dominos', 6, 'https://scontent.fotp3-3.fna.fbcdn.net/v/t1.0-9/48365942_1974458319257672_7851144288023871488_n.png?_nc_cat=104&_nc_ht=scontent.fotp3-3.fna&oh=83876346bab9ca8a919a98bbef3fa79f&oe=5CBD71F2', CURRENT_TIMESTAMP);
INSERT INTO restaurants VALUES (5, 'PizzaHut', 'pizzahut@gmail.com', '0728441130', 'pizzahut', 3, 'https://scontent.fotp3-2.fna.fbcdn.net/v/t1.0-9/18527715_1639913159355482_2840008991308881657_n.png?_nc_cat=106&_nc_ht=scontent.fotp3-2.fna&oh=b47872d06366d27a8faa5282d08f799f&oe=5CC3EC40', CURRENT_TIMESTAMP);
INSERT INTO restaurants VALUES (6, 'Panini', 'panini@gmail.com', '0728441131', 'Panini', 10, 'https://scontent.fotp3-1.fna.fbcdn.net/v/t1.0-9/21077723_1750708831638372_6518113221077813021_n.jpg?_nc_cat=108&_nc_ht=scontent.fotp3-1.fna&oh=227b80ed2ec125fa3e4cabb01ea966d8&oe=5CCD7951', CURRENT_TIMESTAMP);

CREATE TABLE reserve(
    id INT PRIMARY KEY NOT NULL, 
    user_id INT REFERENCES users(id),
    restaurant_id INT REFERENCES restaurants(id),
    date date NOT NULL,
    time TIME NOT NULL,
    created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP,
);

CREATE TABLE reserve(id SERIAL PRIMARY KEY, restaurant_id INT REFERENCES restaurants(id), name VARCHAR(30), phone VARCHAR(20), date VARCHAR(15) NOT NULL, time VARCHAR(10) NOT NULL, created_at TIMESTAMP NOT NULL DEFAULT CURRENT_TIMESTAMP);