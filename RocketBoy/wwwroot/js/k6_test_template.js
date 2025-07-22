// k6_test_template.js

import http from 'k6/http';
import { check, sleep } from 'k6';

// Load test parameters
export const options = {
    vus: __VUS__, // Number of Virtual Users
    duration: '__DURATION__', // Load test duration (e.g., "10s", "1m")
};

// Function to test the specified endpoint
export default function () {
    const url = '__URL__'; // API endpoint
    const method = '__METHOD__'; // HTTP method (GET, POST, etc.)
    const headers = JSON.parse('__HEADERS__'); // Request headers as a JSON object
    const body = '__BODY__'; // Request body (for POST/PUT/PATCH)

    // Prepare the body properly based on the method
    let response;
    if (method === 'GET') {
        response = http.get(url, { headers: headers });
    } else {
        // Use an empty string if body is 'null'
        const requestBody = body === 'null' ? '' : body;
        response = http.request(method, url, requestBody, { headers: headers });
    }

    // Check response status and include additional checks
    check(response, {
        'status is 200': (r) => r.status === 200,
        'response time < 500ms': (r) => r.timings.duration < 500,
    });

    // Pause briefly before the next request
    sleep(1);
}