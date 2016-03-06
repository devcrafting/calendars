(function() {
        
        var app = angular.module("Calendars", []);
        
        app.controller("ActivitiesByMonthViewModel", function($scope, $http) {    
            var self = this;
            
            self.loadingSynthesis = true;
            self.loadingOverloadedDays = true;
            self.months = [];
            self.activities = [];
            self.totals = [];
            self.totalManDays = "-";
            self.totalRevenues = "-";
           
            var loadSynthesis = function() { 
                self.loadingSynthesis = true;
                $http.get('/synthesis').success(function(data) {
                    self.activities = data.activitiesByMonth;
                    self.totals = data.totals;
                    self.months = data.months;
                    self.totalManDays = data.totalManDays;
                    self.totalRevenues = data.totalRevenues;
                    self.loadingSynthesis = false;
                    
                    loadOverloadedDays();
                }); 
            };
            
            var loadOverloadedDays = function() { 
                self.loadingOverloadedDays = true;
                $http.get('/overloadedDays').success(function(data) {
                    self.overloadedDays = data
                    self.loadingOverloadedDays = false;
                }); 
            };
            
            loadSynthesis();
        });
    })();