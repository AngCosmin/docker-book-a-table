var app = angular.module('catsvsdogs', []);
var socket = io.connect({ transports: ['polling'] });

app.controller('statsCtrl', function ($scope) {
	$scope.restaurants = [];
	$scope.restaurantReservations = [];

	socket.on('restaurants', function (json) {
		$scope.restaurants = json;
	})

	socket.on('scores', function (json) {
		console.log(json)

		$scope.$apply(function () {
			$scope.restaurantReservations = json;
		});

		$scope.reservationsSelected = []
		for (let r of $scope.restaurantReservations) {
			if  (r.restaurant_id === $scope.selected_restaurant_id) {
				$scope.$apply(function () {
					$scope.reservationsSelected.push(r)
				});
			}
		}
	});

	$scope.select = function(restaurant_id) {
		$scope.reservationsSelected = []
		$scope.selected_restaurant_id = restaurant_id
    };
});
