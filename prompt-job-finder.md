# Job Finder ADS \- Especificação Completa do Projeto

## Papel

Você é um Arquiteto de Software Staff/Senior especializado em:

- Flutter Desktop
- Dart
- .NET 10
- C\#
- SQLite
- Entity Framework Core
- Clean Architecture
- Domain Driven Design
- Web APIs
- Playwright
- Automação Web
- Background Services
- Machine Learning
- Inteligência Artificial aplicada a RH e Recrutamento
- Testes Automatizados
- DevOps

Sua missão é projetar, implementar e documentar integralmente um sistema desktop que encontre oportunidades de estágio aderentes ao perfil definido abaixo.

---

# Objetivo

Criar um aplicativo desktop inteligente chamado:

**Job Finder ADS**

O sistema deverá monitorar continuamente vagas de estágio e programas de entrada em tecnologia, identificar oportunidades compatíveis e apresentá\-las através de uma interface Flutter Desktop.

Não utilizar Telegram.

Todo o fluxo deve ocorrer dentro do próprio aplicativo.

---

# Perfil da Candidata

Utilize este perfil para calcular aderência das vagas.

## Dados

- Sexo: Feminino
- Idade: 31 anos
- Estado: São Paulo
- Região: Capital e Grande São Paulo
- Curso: Tecnólogo em Análise e Desenvolvimento de Sistemas
- Situação: Cursando
- Experiência: Busca primeira oportunidade formal na área
- Objetivo principal: Experiência profissional
- Bolsa: qualquer valor é aceitável
- Disponibilidade: 
    - Home Office
    - Híbrido
    - Presencial

## Idiomas

Português:

- Nativo

Espanhol:

- B2
- Conversação profissional
- Leitura técnica
- Escrita profissional
- Participação em reuniões

## Necessidades Especiais

TEA nível 1.

A informação deve ser utilizada apenas para identificar empresas com programas de inclusão e neurodiversidade.

Nunca utilizar esse dado para excluir vagas.

---

# Áreas de Interesse

Priorizar oportunidades relacionadas a:

- QA
- Quality Assurance
- Testes de Software
- QA Manual
- QA Automation
- Analista de Testes
- Desenvolvimento de Software
- Desenvolvedor Júnior
- Desenvolvedor Estagiário
- Análise e Desenvolvimento de Sistemas
- Engenharia de Software
- DevOps
- DevSecOps
- SRE
- Cloud Computing
- Suporte Técnico
- Cyber Security
- Segurança da Informação
- Dados
- Business Intelligence
- Tecnologia da Informação

---

# Fontes de Pesquisa

O sistema deve tentar utilizar APIs oficiais sempre que possível.

Caso não existam APIs oficiais, utilizar Playwright.

Monitorar:

- LinkedIn Jobs
- Gupy
- Kenoby
- Catho
- Glassdoor
- Indeed
- Vagas.com.br
- CIEE
- Nube
- InfoJobs
- Revelo
- GeekHunter
- Programathor
- Trabalha Brasil
- Remotar
- Trampos
- Sites corporativos
- Portais universitários
- Qualquer outra fonte pública relevante

---

# Requisitos Funcionais

## RF001

Coletar vagas automaticamente.

---

## RF002

Executar coleta em background.

---

## RF003

Agendamento configurável.

Exemplos:

- A cada 15 minutos
- A cada 30 minutos
- A cada hora

---

## RF004

Identificar novas vagas.

---

## RF005

Remover duplicidades.

---

## RF006

Persistir histórico.

---

## RF007

Atualizar vagas quando modificadas.

---

## RF008

Classificar aderência.

---

## RF009

Gerar resumo automático.

---

## RF010

Exibir vagas em interface desktop.

---

## RF011

Permitir favoritos.

---

## RF012

Controlar status de candidatura.

Status possíveis:

- Não visualizada
- Interessante
- Favoritada
- Candidatura enviada
- Em análise
- Entrevista
- Oferta recebida
- Rejeitada
- Encerrada

---

## RF013

Manter histórico de ações.

---

## RF014

Gerar notificações desktop.

---

## RF015

Permitir abertura do link original da vaga.

---

# Sistema de Compatibilidade

Calcular score de 0 a 100.

Sugestão:

ADS cursando: 30%

Estágio: 20%

Área desejada: 20%

Localização: 10%

Espanhol: 10%

Inclusão e diversidade: 10%

Classificação:

90\-100: Excelente

75\-89: Muito Boa

60\-74: Boa

0\-59: Baixa

---

# Regras de Priorização

Conceder score adicional para vagas contendo:

- Espanhol
- Spanish
- Español
- LATAM
- América Latina
- Regional Support
- International Team
- Global Team
- Multinational Company
- Mulheres na Tecnologia
- Women in Tech
- Programa Inclusivo
- Neurodiversidade

---

# Resumo Inteligente

Para cada vaga gerar:

## Resumo

Descrição simplificada da oportunidade.

## Motivos de Compatibilidade

Exemplo:

✓ Curso compatível

✓ Espanhol valorizado

✓ Área QA

✓ Sem experiência obrigatória

✓ Home Office

---

# Interface Flutter Desktop

Utilizar Material 3.

---

# Dashboard Inicial

Exibir:

- Total de vagas coletadas
- Novas vagas hoje
- Favoritas
- Candidaturas ativas
- Taxa de aderência média
- Fontes monitoradas
- Última atualização

---

# Tela de Vagas

Mostrar cards contendo:

- Cargo
- Empresa
- Localização
- Modalidade
- Bolsa
- Fonte
- Data
- Compatibilidade
- Resumo

Botões:

- Favoritar
- Abrir vaga
- Marcar candidatura
- Ocultar

---

# Tela de Filtros

Permitir filtrar por:

Área

Modalidade

Empresa

Local

Remoto

Híbrido

Presencial

Faixa de aderência

Necessário Espanhol

Empresa inclusiva

---

# Tela de Favoritos

Listar todas as vagas favoritas.

---

# Tela de Candidaturas

Acompanhar processo seletivo.

---

# Tela de Estatísticas

Mostrar:

- Vagas por fonte
- Vagas por área
- Vagas por modalidade
- Empresas que mais publicam
- Evolução mensal

Utilizar gráficos.

---

# Tela de Configurações

Intervalo de atualização

Fontes habilitadas

Quantidade máxima de registros

Banco de dados

Logs

Tema claro/escuro

---

# Notificações Desktop

Quando surgir vaga com score acima de 85.

Exemplo:

Nova vaga encontrada

QA Intern \- IBM

Compatibilidade: 94%

Clique para abrir.

---

# Banco de Dados

Utilizar SQLite.

Criar migrations.

Criar índices.

Criar seed opcional.

---

# Estrutura da Solução

## Frontend

Flutter Desktop

Arquitetura:

- Feature First
- Clean Architecture
- Repository Pattern
- Dependency Injection

---

## Backend

.NET 10

Arquitetura:

- Clean Architecture
- DDD
- CQRS quando fizer sentido

Projetos:

- Domain
- Application
- Infrastructure
- Persistence
- API
- Worker

---

# Tecnologias

Frontend:

- Flutter
- Dart
- Material 3
- Riverpod

Backend:

- .NET 10
- ASP.NET Core
- EF Core
- SQLite

Logs:

- Serilog

Automação:

- Playwright

Testes:

- xUnit
- FluentAssertions

---

# Requisitos Não Funcionais

Implementar:

- Retry Policy
- Cache
- Rate Limiting
- Logging
- Tratamento de Falhas
- Observabilidade
- Performance
- Escalabilidade

---

# Segurança

Nunca armazenar credenciais em código.

Utilizar:

- appsettings
- secrets
- variáveis de ambiente

---

# Entregáveis

Gerar:

1. Estrutura completa da solução
2. Código\-fonte completo
3. Backend funcional
4. Frontend funcional
5. Banco SQLite
6. Migrations
7. Testes unitários
8. Testes de integração
9. Diagramas de arquitetura
10. README detalhado
11. Guia de instalação
12. Guia de execução
13. Guia de manutenção
14. Guia de extensão para novas fontes
15. Dockerfiles do backend
16. Scripts de inicialização
17. Exemplos de configuração
18. Roadmap de melhorias futuras

---

# Estratégia de Desenvolvimento

Desenvolver em etapas.

Ao final de cada etapa:

- explicar as decisões arquiteturais
- gerar código compilável
- evitar pseudocódigo
- evitar exemplos simplificados
- entregar implementação pronta para produção

Objetivo final: possuir um agregador inteligente de vagas de estágio em tecnologia totalmente funcional para Windows Desktop, baseado em Flutter \+ .NET 10, capaz de encontrar, analisar, classificar e gerenciar oportunidades aderentes ao perfil informado.

