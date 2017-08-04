$(document).ready(function () {
    $('[data-toggle="tooltip"]').tooltip();
    var GetDonutAndBarGraphDataURL = '/Home/GetDonutAndBarGraphData';
    var monthNames = ["January", "February", "March", "April", "May", "June",
  "July", "August", "September", "October", "November", "December"
    ];
    //Sidebar hover for editing user info
    $("#UserArea").hover(
              function () {
                  $(this).find('#userEdit').show();
              }, function () {
                  $(this).find('#userEdit').hide();
              }
            );
    //This code is used to make the sidebar mobile friendly and visible on a click
    $("#showSidebar").on('click', function () {
        if (!$('#hoveringSidebar #sidebar').length) {
            $('#sidebar').clone().appendTo('#hoveringSidebar');
            $('#hoveringSidebar #sidebar').removeAttr('class');
            $('[data-toggle="tooltip"]').tooltip();
            $('.myWarning button').on('click', function () {
                $(this).parent().fadeOut();
            });
            $("#UserArea").hover(
              function () {
                  $(this).find('#userEdit').show();
              }, function () {
                  $(this).find('#userEdit').hide();
              }
            );
           
        }
        $('#closeSideBar').on("click", function () {
            $('#hoveringSidebar').hide("slide", { direction: "left" }, 500);
            $("#showSidebar").removeClass('sidebarToggled', 500);
        })
        $(this).toggleClass('sidebarToggled', 500);
        if ($(this).hasClass('sidebarToggled')) {
            $('#hoveringSidebar').hide("slide", { direction: "left" }, 500);
        } else {
            $('#hoveringSidebar').show("slide", { direction: "left" }, 500);
        }
    })
    var getWarningsURL = '/Home/getWarnings';

    $.ajax({
        async: true,
        url: getWarningsURL,
        dataType: "json",
        success: function (json) {
            $.each(json, function (index, value) {
                $('#warnings').hide().append("<div id='"+index+"' class='alert alert-danger myWarning'><button type='button' aria-hidden='true' class='close'>×</button><p>" + value +"</p></div>").show('fast');
            })
            $('.myWarning button').on('click', function () {
                $(this).parent().fadeOut();
            })
        },
        error: function (e) {
            //alert("Warnings data request failed");
        }
    });

    var getNotificationsURL = '/Home/GetNewlyEarnedBadges';

    $.ajax({
        async: true,
        url: getNotificationsURL,
        dataType: "json",
        success: function (json) {
            $.each(json, function (index, value) {
                $.notify({
                    icon: "images/" + value.url,
                    title: "Badge Earned!",
                    message: "   " + value.descr + " - <i>" + value.date + "</i>"
                }, {
                    type: 'minimalist',
                    delay: 20000,
                    placement: {
                        from: 'bottom',
                        align: 'right'
                    },
                    animate: {
                        enter: 'animated bounceIn',
                        exit: 'animated fadeOutDown'
                    },
                    icon_type: 'image',
                    template: '<div data-notify="container" class=" alert-success col-xs-11 col-sm-3 alert alert-{0}" role="alert">' +
                            '<button type="button" aria-hidden="true" class="close" data-notify="dismiss">×</button>' +
		                    '<img data-notify="icon" class="img-circle pull-left" width="40" height="40">' +
		                    '<span data-notify="title">{1}</span>' +
		                    '<span data-notify="message">{2}</span>' +
	                        '</div>'

                });
            })
            
        },
        error: function (e) {
            //alert("New Badges data request failed");
        }
    });
    ctx = $("#chartArea")[0];

    if (ctx != null) {
        // getting data for donut and bar graph
        var config;
        $.ajax({
            async: true,
            url: GetDonutAndBarGraphDataURL,
            dataType: "json",
            success: function (json) {
                var d = new Date();
                var month = d.getMonth();
                config = {
                    type: 'doughnut',
                    data: json,
                    options: {
                        responsive: true,
                        legend: {
                            position: 'bottom',
                            labels: {
                                boxWidth: 10
                            }
                        },
                        title: {
                            display: true,
                            text: monthNames[month] + '\'s Spending'
                        },
                        animation: {
                            animateScale: true,
                            animateRotate: true
                        },
                        tooltips: {
                            callbacks: {
                                title: function (tooltipItem, data) {
                                    return data.labels[tooltipItem[0].index];
                                },
                                //footer: function (tooltipItem, data) {
                                //    return "footer";
                                //},
                                label: function (tooltipItem, data) {
                                    //get the concerned dataset
                                    var dataset = data.datasets[tooltipItem.datasetIndex];
                                    //calculate the total of this data set
                                    var total = dataset.data[tooltipItem.index];


                                    return " " + currencyFormat(total, '$');
                                }
                            }
                        }
                    }
                };
                if (json.datasets[0].data.length == 0) {
                    window.graphContainer.innerHTML = "<div id='noGraphData'>You have not spent any money this month! </div>";
                } else {
                    window.myChart = new Chart(ctx, config);
                }

                afterResizing();
            },
            error: function () {
                //alert("Transactions 2 data request failed");
            }
        });
        $('#showCharts li span').on('click', function () {
            $(this).parent().siblings().find('span').show();
            $(this).hide();
            var type = $(this).attr("data-show");
            change(type);
            var text = $(this).text();
            $('#currentType').text(text);
        })

        function change(chartType) {
            var ctx = document.getElementById("chartArea").getContext("2d");

            if (window.myChart) {
                window.myChart.destroy();
            }
            var temp = $.extend(true, {}, config);
            temp.type = chartType;
            if (chartType == 'line' || chartType == 'bar') {
                var scale = {
                        yAxes: [{
                            ticks: {
                                beginAtZero: true
                            }
                        }]
                };
                temp.options.scales = scale;
            }
            if (chartType == 'line') {
                temp.data.datasets[0].fill = false;
            }
            window.myChart = new Chart(ctx, temp);
        }


        var resizeId;
        $(window).resize(function () {
            clearTimeout(resizeId);
            resizeId = setTimeout(afterResizing, 100);
        });
        function afterResizing() {
            var canvasheight = $("#chartArea").height();
            if (canvasheight <= 375) {
                window.myChart.options.legend.display = false;
            } else {
                window.myChart.options.legend.display = true;
                window.myChart.options.legend.position = 'bottom';
            }
            window.myChart.update();
        }
    }
});

function currencyFormat(n, currency) {
    return currency + n.toFixed(2).replace(/(\d)(?=(\d{3})+\.)/g, "$1,");
}