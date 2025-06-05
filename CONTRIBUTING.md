# Contributing to SSE Demo

First off, thank you for considering contributing to this SSE demo project! This project serves as an educational example of implementing Server-Sent Events correctly with modern technologies and Kubernetes scalability.

## Code of Conduct

This project and everyone participating in it is governed by our Code of Conduct. By participating, you are expected to uphold this code. Please report unacceptable behavior to the project maintainers.

## How Can I Contribute?

### Reporting Bugs

Before creating bug reports, please check existing issues as you might find out that you don't need to create one. When you are creating a bug report, please include as many details as possible:

- **Use a clear and descriptive title**
- **Describe the exact steps to reproduce the problem**
- **Provide specific examples to demonstrate the steps**
- **Describe the behavior you observed after following the steps**
- **Explain which behavior you expected to see instead and why**
- **Include logs and error messages**
- **Include your environment details** (OS, .NET version, Node.js version, etc.)

### Suggesting Enhancements

Enhancement suggestions are tracked as GitHub issues. When creating an enhancement suggestion, please include:

- **Use a clear and descriptive title**
- **Provide a step-by-step description of the suggested enhancement**
- **Provide specific examples to demonstrate the steps**
- **Describe the current behavior and explain which behavior you expected to see instead**
- **Explain why this enhancement would be useful**

### Pull Requests

1. Fork the repo and create your branch from `main`
2. If you've added code that should be tested, add tests
3. If you've changed APIs, update the documentation
4. Ensure the test suite passes
5. Make sure your code follows the existing code style
6. Issue that pull request!

## Development Process

### Setting Up Your Development Environment

1. **Prerequisites**
   - .NET 9.0 SDK or later
   - Node.js 14+ and npm
   - Docker Desktop (for local Kubernetes testing)
   - Git

2. **Fork and Clone**
   ```bash
   git clone https://github.com/your-username/sse-realtime-demo.git
   cd sse-realtime-demo
   ```

3. **Backend Setup**
   ```bash
   cd backend
   dotnet restore
   dotnet build
   ```

4. **Frontend Setup**
   ```bash
   cd frontend
   npm install
   ```

### Running Tests

```bash
# Backend tests
cd backend
dotnet test

# Frontend tests
cd frontend
npm test
```

### Code Style Guidelines

#### C# (.NET Backend)

- Follow [Microsoft's C# Coding Conventions](https://docs.microsoft.com/en-us/dotnet/csharp/fundamentals/coding-style/coding-conventions)
- Use meaningful variable and method names
- Keep methods focused and small
- Add XML documentation comments for public APIs
- Use async/await for asynchronous operations
- Follow SOLID principles

#### TypeScript/React (Frontend)

- Follow the existing code style in the project
- Use functional components with hooks
- Properly type all props and state
- Use meaningful component and variable names
- Keep components focused and reusable
- Add JSDoc comments for complex logic

#### General Guidelines

- Write self-documenting code
- Add comments only when necessary to explain "why" not "what"
- Keep files focused on a single responsibility
- Test your changes thoroughly
- Update documentation when changing functionality

### Commit Message Guidelines

We follow a simplified version of Conventional Commits:

- `feat:` New feature
- `fix:` Bug fix
- `docs:` Documentation changes
- `style:` Code style changes (formatting, etc.)
- `refactor:` Code refactoring
- `test:` Test additions or corrections
- `chore:` Maintenance tasks

Examples:
```
feat: add typed event support for SSE client
fix: resolve connection timeout in Kubernetes deployment
docs: update README with production scaling guidelines
```

### Documentation

- Update the README.md if you change functionality
- Add inline documentation for complex logic
- Update architecture diagrams if you change the system design
- Include examples for new features

## Project Structure

```
sse-realtime-demo/
├── backend/           # .NET 9.0 SSE server implementation
├── frontend/          # React TypeScript SSE client
├── k8s/              # Kubernetes deployment configurations
├── LICENSE           # MIT License
├── README.md         # Project documentation
└── CONTRIBUTING.md   # This file
```

## Areas for Contribution

We're particularly interested in contributions in these areas:

1. **Performance Improvements**
   - Connection pooling optimizations
   - Event batching strategies
   - Memory usage optimization

2. **Additional Examples**
   - Different SSE use cases
   - Integration with other services
   - Advanced filtering patterns

3. **Monitoring and Observability**
   - Prometheus metrics
   - Distributed tracing
   - Performance benchmarks

4. **Security Enhancements**
   - JWT authentication example
   - Rate limiting
   - DDoS protection strategies

5. **Documentation**
   - Additional architecture diagrams
   - Performance tuning guides
   - Troubleshooting guides

## Questions?

Feel free to open an issue with your question or reach out to the maintainers directly.

Thank you for contributing! 🎉