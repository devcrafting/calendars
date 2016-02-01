(function() {
        
        var app = angular.module("Calendars", []);
        
        app.controller("ActivitiesByMonthViewModel", function($scope, $http) {    
            var self = this;
            
            self.text = "";
            self.months = [];
            self.activities = [];
            self.totals = [];
            self.totalManDays = "-";
            self.totalRevenues = "-";
           
            var load = function() { 
                $http.get('/synthesis').success(function(data) {
                    self.activities = data.activitiesByMonth;
                    self.totals = data.totals;
                    self.months = data.months;
                }); 
            };
            
            load();
        });
    })();