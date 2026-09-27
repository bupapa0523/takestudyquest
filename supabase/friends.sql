create table if not exists public.friend_requests (
  id text primary key,
  from_code text not null,
  to_code text not null,
  from_name text,
  status text not null default 'pending',
  room text not null default 'takestudy',
  created_at timestamptz not null default now()
);

create table if not exists public.study_posts (
  id text primary key,
  author_code text not null,
  author_name text,
  material_name text,
  subject int not null default 0,
  minutes int not null default 0,
  started_at text,
  room text not null default 'takestudy'
);

alter table public.friend_requests enable row level security;
alter table public.study_posts enable row level security;

drop policy if exists "friend_requests_read" on public.friend_requests;
drop policy if exists "friend_requests_insert" on public.friend_requests;
drop policy if exists "friend_requests_update" on public.friend_requests;
drop policy if exists "study_posts_read" on public.study_posts;
drop policy if exists "study_posts_insert" on public.study_posts;
drop policy if exists "study_posts_update" on public.study_posts;

create policy "friend_requests_read" on public.friend_requests for select using (true);
create policy "friend_requests_insert" on public.friend_requests for insert with check (true);
create policy "friend_requests_update" on public.friend_requests for update using (true) with check (true);
create policy "friend_requests_delete" on public.friend_requests for delete using (true);
create policy "study_posts_read" on public.study_posts for select using (true);
create policy "study_posts_insert" on public.study_posts for insert with check (true);
create policy "study_posts_update" on public.study_posts for update using (true) with check (true);

grant select, insert, update, delete on public.friend_requests to anon, authenticated;
grant select, insert, update on public.study_posts to anon, authenticated;
