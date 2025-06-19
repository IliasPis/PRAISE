const express = require('express');
const bodyParser = require('body-parser');
const axios = require('axios');

const app = express();
const PORT = 3000;

// Replace with your Supabase service role key
const SUPABASE_SERVICE_ROLE_KEY = 'your-service-role-key';
const SUPABASE_URL = 'https://nhtwbrztvgchuefjjzgt.supabase.co';

app.use(bodyParser.json());

app.put('/change-password', async (req, res) => {
    const { email, password } = req.body;

    if (!email || !password) {
        return res.status(400).json({ error: 'Email and password are required' });
    }

    try {
        const response = await axios.put(
            `${SUPABASE_URL}/auth/v1/admin/users`,
            { email, password },
            {
                headers: {
                    'Content-Type': 'application/json',
                    apikey: SUPABASE_SERVICE_ROLE_KEY,
                    Authorization: `Bearer ${SUPABASE_SERVICE_ROLE_KEY}`,
                },
            }
        );

        res.status(200).json({ message: 'Password changed successfully', data: response.data });
    } catch (error) {
        console.error('[ChangePassword] Error:', error.response?.data || error.message);
        res.status(error.response?.status || 500).json({ error: error.response?.data || error.message });
    }
});

app.listen(PORT, () => {
    console.log(`Server running on http://localhost:${PORT}`);
});
