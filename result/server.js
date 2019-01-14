var express = require('express'),
	async = require('async'),
	pg = require("pg"),
	path = require("path"),
	cookieParser = require('cookie-parser'),
	bodyParser = require('body-parser'),
	methodOverride = require('method-override'),
	app = express(),
	server = require('http').Server(app),
	io = require('socket.io')(server);

io.set('transports', ['polling']);

var port = process.env.PORT || 4000;
var clientSql = null

io.sockets.on('connection', function (socket) {
	setTimeout(function() {
		clientSql.query('SELECT * FROM restaurants', [], function (err, result) {
			if (err) {
				console.error("Error performing query: " + err);
			} else {
				socket.emit("restaurants", result.rows);
			}
		});
	}, 1000)
	

	socket.on('subscribe', function (data) {
		socket.join(data.channel);
	});
});

async.retry(
	{ times: 1000, interval: 1000 },
	function (callback) {
		pg.connect('postgres://postgres@db/postgres', function (err, client, done) {
			if (err) {
				console.error("Waiting for db");
			}
			callback(err, client);
		});
	},
	function (err, client) {
		if (err) {
			return console.error("Giving up");
		}
		clientSql = client
		console.log("Connected to db");
		getVotes(client);
	}
);

function getVotes(client) {
	client.query('SELECT * FROM reserve;', [], function (err, result) {
		if (err) {
			console.error("Error performing query: " + err);
		} else {
			io.sockets.emit("scores", result.rows);
		}

		setTimeout(function () { getVotes(client) }, 1000);
	});
}

app.use(cookieParser());
app.use(bodyParser());
app.use(methodOverride('X-HTTP-Method-Override'));
app.use(function (req, res, next) {
	res.header("Access-Control-Allow-Origin", "*");
	res.header("Access-Control-Allow-Headers", "Origin, X-Requested-With, Content-Type, Accept");
	res.header("Access-Control-Allow-Methods", "PUT, GET, POST, DELETE, OPTIONS");
	next();
});

app.use(express.static(__dirname + '/views'));

app.get('/', function (req, res) {
	res.sendFile(path.resolve(__dirname + '/views/index.html'));
});

server.listen(port, function () {
	var port = server.address().port;
	console.log('App running on port ' + port);
});
