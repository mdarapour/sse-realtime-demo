# Security Notice

## ⚠️ This is a DEMO Application

This application is designed for **educational and demonstration purposes only**. It intentionally includes simplified security configurations to make it easy to run and understand.

### Known Security Simplifications

1. **Hardcoded API Keys**
   - Demo API keys are included in configuration files
   - These are meant for local development only

2. **Basic MongoDB Authentication**
   - Uses simple credentials (admin/admin123) for ease of setup
   - MongoDB runs without authentication in the default configuration

3. **Debug Logging**
   - Verbose logging is enabled by default for educational purposes
   - This may expose application internals

4. **CORS Configuration**
   - Permissive CORS settings for easy frontend development
   - Allows credentials from multiple origins

### For Production Use

If you plan to use this code as a basis for a production application, you MUST:

- [ ] Replace all hardcoded credentials with environment variables
- [ ] Implement proper authentication (OAuth2, JWT, etc.)
- [ ] Use strong, randomly generated passwords
- [ ] Enable MongoDB authentication with proper credentials
- [ ] Restrict CORS origins to your specific domains
- [ ] Set logging to "Information" or "Warning" level
- [ ] Implement rate limiting and request validation
- [ ] Use HTTPS for all connections
- [ ] Implement proper secret management (Azure Key Vault, AWS Secrets Manager, etc.)
- [ ] Add input validation and sanitization
- [ ] Implement proper error handling without exposing internals

### Reporting Security Issues

Since this is a demo application, security issues related to the intentionally simplified security model will not be addressed. However, if you find security issues in the core SSE implementation logic, please open an issue.