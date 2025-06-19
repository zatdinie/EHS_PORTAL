// date-helpers.js - Simple date formatting utilities to replace moment.js

window.DateHelpers = {
    // Format a date into various string formats
    formatDate: function(date, format) {
        if (!date) return '';
        
        const d = new Date(date);
        if (isNaN(d.getTime())) return '';
        
        // Common format patterns
        switch (format) {
            case 'MMM D, YYYY':
                return `${this.getMonthShort(d.getMonth())} ${d.getDate()}, ${d.getFullYear()}`;
            case 'MMMM D, YYYY':
                return `${this.getMonthLong(d.getMonth())} ${d.getDate()}, ${d.getFullYear()}`;
            case 'dddd, MMMM D, YYYY':
                return `${this.getDayLong(d.getDay())}, ${this.getMonthLong(d.getMonth())} ${d.getDate()}, ${d.getFullYear()}`;
            case 'YYYY-MM-DD':
                return `${d.getFullYear()}-${this.padZero(d.getMonth() + 1)}-${this.padZero(d.getDate())}`;
            case 'ddd':
                return this.getDayShort(d.getDay());
            default:
                return d.toLocaleDateString();
        }
    },
    
    // Get days difference between two dates
    daysDiff: function(date1, date2) {
        const d1 = this.startOfDay(new Date(date1));
        const d2 = this.startOfDay(new Date(date2));
        const diffTime = d2 - d1;
        return Math.round(diffTime / (1000 * 60 * 60 * 24));
    },
    
    // Start of day (00:00:00)
    startOfDay: function(date) {
        const d = new Date(date);
        d.setHours(0, 0, 0, 0);
        return d;
    },
    
    // Add days to a date
    addDays: function(date, days) {
        const d = new Date(date);
        d.setDate(d.getDate() + days);
        return d;
    },
    
    // Month names (short)
    getMonthShort: function(month) {
        const months = ['Jan', 'Feb', 'Mar', 'Apr', 'May', 'Jun', 'Jul', 'Aug', 'Sep', 'Oct', 'Nov', 'Dec'];
        return months[month];
    },
    
    // Month names (long)
    getMonthLong: function(month) {
        const months = ['January', 'February', 'March', 'April', 'May', 'June', 'July', 'August', 'September', 'October', 'November', 'December'];
        return months[month];
    },
    
    // Day names (short)
    getDayShort: function(day) {
        const days = ['Sun', 'Mon', 'Tue', 'Wed', 'Thu', 'Fri', 'Sat'];
        return days[day];
    },
    
    // Day names (long)
    getDayLong: function(day) {
        const days = ['Sunday', 'Monday', 'Tuesday', 'Wednesday', 'Thursday', 'Friday', 'Saturday'];
        return days[day];
    },
    
    // Pad with leading zero
    padZero: function(num) {
        return num < 10 ? '0' + num : num;
    }
}; 