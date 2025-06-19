// EHS Portal JavaScript

document.addEventListener('DOMContentLoaded', function() {
    // Add hover effect to cards
    const cards = document.querySelectorAll('.card');
    
    cards.forEach(card => {
        card.addEventListener('mouseenter', function() {
            this.style.transform = 'translateY(-10px)';
            this.style.boxShadow = '0 15px 30px rgba(0, 0, 0, 0.15)';
        });
        
        card.addEventListener('mouseleave', function() {
            this.style.transform = 'translateY(0)';
            this.style.boxShadow = '0 4px 8px rgba(0, 0, 0, 0.1)';
        });
        
        // Log clicks for analytics
        card.addEventListener('click', function() {
            const cardTitle = this.querySelector('.card-title').textContent;
            console.log(`Card clicked: ${cardTitle}`);
            // In a real application, you might send this to an analytics service
        });
    });

    // Update safety statistics
    updateSafetyStats();

    // Get current year for footer copyright
    const currentYear = new Date().getFullYear();
    const copyrightElement = document.querySelector('.footer-bottom p');
    if (copyrightElement) {
        copyrightElement.textContent = copyrightElement.textContent.replace('2025', currentYear);
    }
}); 

// Function to update safety statistics
function updateSafetyStats() {
    // Configuration - Change this date to adjust the "last accident" date
    const lastAccidentDate = new Date('2025-03-01'); // Format: YYYY-MM-DD
    
    // Calculate days since last accident
    const today = new Date();
    const timeDiff = today - lastAccidentDate;
    const daysSinceAccident = Math.floor(timeDiff / (1000 * 60 * 60 * 24));
    
    // Set total accidents this year - hardcoded value
    const totalAccidentsThisYear = 0; // Hard-coded value
    
    // Animate the counters
    animateCountUp('days-since-accident', daysSinceAccident);
    animateCountUp('total-accidents', totalAccidentsThisYear);
}

// Function to animate count up effect
function animateCountUp(elementId, targetValue) {
    const element = document.getElementById(elementId);
    if (!element) return;
    
    // Start from 0
    let currentValue = 0;
    element.textContent = '0';
    
    // Calculate animation duration and step based on target value
    const duration = 2500; // ms
    const steps = 50;
    const stepValue = targetValue / steps;
    const stepDuration = duration / steps;
    
    // Animate
    const counter = setInterval(() => {
        currentValue += stepValue;
        
        // If we've reached or exceeded the target, set the final value and clear interval
        if (currentValue >= targetValue) {
            element.textContent = targetValue;
            clearInterval(counter);
            return;
        }
        
        // Set the current value, rounded to nearest integer
        element.textContent = Math.round(currentValue);
    }, stepDuration);
} 