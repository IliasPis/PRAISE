const express = require('express');
const bodyParser = require('body-parser');
const jwt = require('jsonwebtoken');

const app = express();
const port = 3000;
const users = {}; // In-memory user store, replace with a database in production
const secretKey = 'puFlu8AtkWXvJoBIo8V6ffM104KYrnpg'; // Replace with a secure key in production

app.use(bodyParser.json());

app.post('/register', (req, res) => {
    const { customId, fullName, password } = req.body;

    if (!customId || !fullName || !password) {
        return res.status(400).send('Custom ID, full name, and password are required.');
    }

    if (users[customId]) {
        return res.status(400).send('User already exists.');
    }

    users[customId] = { customId, fullName, password };
    res.status(201).send('User registered successfully.');
});

app.post('/login', (req, res) => {
    const { customId, password } = req.body;

    if (!customId || !password) {
        return res.status(400).send('Custom ID and password are required.');
    }

    const user = users[customId];
    if (!user || user.password !== password) {
        return res.status(401).send('Invalid custom ID or password.');
    }

    const token = jwt.sign({ customId }, secretKey, { expiresIn: '1h' });
    res.status(200).send({ token });
});

app.post('/change-password', (req, res) => {
    const { customId, newPassword } = req.body;
    const token = req.headers.authorization?.split(' ')[1];

    if (!token) {
        return res.status(401).send('Authorization token is required.');
    }

    try {
        const decoded = jwt.verify(token, secretKey);
        if (decoded.customId !== customId) {
            return res.status(401).send('Invalid token.');
        }

        if (!users[customId]) {
            return res.status(404).send('User not found.');
        }

        users[customId].password = newPassword;
        res.status(200).send('Password changed successfully.');
    } catch (error) {
        res.status(401).send('Invalid token.');
    }
});

app.listen(port, () => {
    console.log(`Backend service running at http://localhost:${port}`);
});
