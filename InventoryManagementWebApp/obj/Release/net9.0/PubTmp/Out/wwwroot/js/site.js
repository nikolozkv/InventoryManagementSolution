// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

// ფუნქცია, რომელიც ზრდის ტექსტარეას სიმაღლეს ტექსტის მიხედვით
function autoHeight(element) {
    if (!element) return;
    element.style.height = "31px"; // დააბრუნე საწყისზე
    element.style.height = (element.scrollHeight) + "px"; // გაზარდე შიგთავსის ზომამდე
}

// გვერდის ჩატვირთვისას ყველა ასეთი ველის გასწორება
document.addEventListener("DOMContentLoaded", function () {
    const textAreas = document.querySelectorAll('.auto-height-textarea');
    textAreas.forEach(el => autoHeight(el));
});
