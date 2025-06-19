// EHS Portal JavaScript

document.addEventListener('DOMContentLoaded', function() {
    // Initialize AOS animations
    AOS.init({
        duration: 800,
        once: true,
        offset: 100
    });

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

    // Initialize announcement banner close button
    const announcementClose = document.querySelector('.announcement-close');
    if (announcementClose) {
        announcementClose.addEventListener('click', function() {
            const banner = document.querySelector('.announcement-banner');
            banner.style.height = banner.offsetHeight + 'px';
            
            // Force reflow
            banner.offsetHeight;
            
            banner.style.height = '0';
            banner.style.opacity = '0';
            banner.style.overflow = 'hidden';
            banner.style.transition = 'height 0.3s ease, opacity 0.3s ease, padding 0.3s ease';
            banner.style.padding = '0';
            
            // Remove from DOM after animation
            setTimeout(() => {
                banner.remove();
            }, 300);
        });
    }

    // Smooth scroll for anchor links
    document.querySelectorAll('a[href^="#"]').forEach(anchor => {
        anchor.addEventListener('click', function(e) {
            const target = document.querySelector(this.getAttribute('href'));
            if (target) {
                e.preventDefault();
                window.scrollTo({
                    top: target.offsetTop - 80, // Offset for header
                    behavior: 'smooth'
                });
            }
        });
    });

    // Get current year for footer copyright
    const currentYear = new Date().getFullYear();
    const copyrightElement = document.querySelector('.footer-bottom p');
    if (copyrightElement) {
        copyrightElement.textContent = copyrightElement.textContent.replace('2025', currentYear);
    }

    // Progress bar animation removed as it's no longer needed
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
    animateCountUp('safety-inspections', 42); // Hardcoded value for safety inspections
}

// Function to animate count up effect
function animateCountUp(elementId, targetValue) {
    const element = document.getElementById(elementId);
    if (!element) return;
    
    // Start from 0
    let currentValue = 0;
    element.textContent = '0';
    
    // Calculate animation duration and step based on target value
    const duration = 5000; // ms
    const steps = 100;
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

// Function to animate progress bar - removed since we no longer use the progress bar

// Function to show a toast notification
function showToast(message, type = 'info') {
    // Create toast element
    const toast = document.createElement('div');
    toast.className = `toast toast-${type}`;
    toast.innerHTML = `
        <div class="toast-content">
            <span>${message}</span>
            <button class="toast-close">&times;</button>
        </div>
    `;
    
    // Add to document
    document.body.appendChild(toast);
    
    // Force reflow
    toast.offsetHeight;
    
    // Show toast
    toast.classList.add('show');
    
    // Add close functionality
    const closeBtn = toast.querySelector('.toast-close');
    closeBtn.addEventListener('click', () => {
        toast.classList.remove('show');
        setTimeout(() => {
            toast.remove();
        }, 300);
    });
    
    // Auto hide after 5 seconds
    setTimeout(() => {
        toast.classList.remove('show');
        setTimeout(() => {
            toast.remove();
        }, 300);
    }, 5000);
} 