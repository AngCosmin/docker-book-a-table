from flask import Flask, render_template, request, make_response, g, jsonify
from redis import Redis
import os
import socket
import random
import json

hostname = socket.gethostname()
app = Flask(__name__)

def get_redis():
    if not hasattr(g, 'redis'):
        g.redis = Redis(host="redis", db=0, socket_timeout=5)
    return g.redis


@app.route("/", methods=['POST','GET'])
def hello():
    redis = get_redis()
    redis.rpush('get_restaurants', '')

    resp = make_response(render_template(
        'index.html',
        hostname=hostname,
    ))
    return resp


@app.route('/get/restaurants', methods=['POST'])
def get_restaurants():
    redis = get_redis()
    restaurants = json.loads(redis.get('restaurants'))

    return jsonify({
        'success': True,
        'restaurants': restaurants,
    })

@app.route('/reserve', methods=['POST'])
def reserve():
    redis = get_redis()

    # TODO: Check data for errors

    redis.rpush('reserve', request.data)

    return jsonify({
        'success': True,
    })


if __name__ == "__main__":
    app.run(host='0.0.0.0', port=80, debug=True, threaded=True)
    redis.rpush('get_restaurants', '')
