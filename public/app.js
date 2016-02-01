(function() {
        
        var app = angular.module("Calendars", []);
        
        app.controller("ActivitiesByMonthViewModel", function($scope, $http) {    
            var self = this;
            
            self.loading = true;
            self.months = [];
            self.activities = [];
            self.totals = [];
            self.totalManDays = "-";
            self.totalRevenues = "-";
           
            var load = function() { 
                self.loading = true;
                $http.get('/synthesis').success(function(data) {
                    self.activities = data.activitiesByMonth;
                    self.totals = data.totals;
                    self.months = data.months;
                    self.loading = false;
                }); 
            };
            
            load();
        });
    })();