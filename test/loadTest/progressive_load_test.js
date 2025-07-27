import http from 'k6/http';
import {check, sleep} from 'k6';

export let options = {
    stages: [
        {duration: '1m', target: 10},
        {duration: '2m', target: 50},
        {duration: '1m', target: 100},
        {duration: '1m', target: 10},
    ]
}

export default function () {
    let customerId = getCustomerId();
    let res = http.get(`https://localhost:7236/api/v1/customers/${customerId}`);
    check(res, {
        'status is 200': (r) => r.status === 200
    });
    sleep(0.01);
}

function getCustomerId() {
    return Math.floor(Math.random() * 10000);
}